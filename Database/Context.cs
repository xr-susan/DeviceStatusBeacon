using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace DeviceStatusBeacon.Database;

/// <inheritdoc/>
public class DeviceStatusBeaconContext(DbContextOptions<DeviceStatusBeaconContext> options)
	: IdentityDbContext<User, IdentityRole<Guid>, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>(options) {
	/// <summary>
	/// 存储在线日志 <seealso cref="OnlineLog"/> 的实体集合，由 EF Core 自动映射到数据库表
	/// </summary>
	public DbSet<OnlineLog> OnlineLogs { get; set; }

	/// <summary>
	/// 存储设备 <seealso cref="Device"/> 的实体集合，由 EF Core 自动映射到数据库表
	/// </summary>
	public DbSet<Device> Devices { get; set; }

	/// <summary>
	/// 存储 API 凭据 <seealso cref="ApiCredential"/> 的实体集合，由 EF Core 自动映射到数据库表
	/// </summary>
	public DbSet<ApiCredential> ApiCredentials { get; set; }

	/// <summary>
	/// 存储 API 凭据到设备授权关系 <seealso cref="ApiCredentialDevice"/> 的实体集合，由 EF Core 自动映射到数据库表
	/// </summary>
	public DbSet<ApiCredentialDevice> ApiCredentialDevices { get; set; }

	/// <summary>
	/// 存储设备到用户授权关系 <seealso cref="DeviceUser"/> 的实体集合，由 EF Core 自动映射到数据库表
	/// </summary>
	public DbSet<DeviceUser> DeviceUsers { get; set; }

	/// <summary>
	/// 存储设置 <seealso cref="SettingInDb"/> 的实体集合，由 EF Core 自动映射到数据库表
	/// </summary>
	public DbSet<SettingInDb> Settings { get; set; }

	/// <inheritdoc/>
	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		// 配置 Device 实体：声明触发器、索引，以及用户到设备的授权关系
		modelBuilder.Entity<Device>(deviceBuilder => {
			// Device 表存在 SQLite AFTER 触发器，因此关闭 RETURNING 子句以回退到兼容触发器的更新 SQL
			deviceBuilder.ToTable(tableBuilder => {
				tableBuilder.HasTrigger(SqliteTriggerNames.DevicesEntityAuthInfoVersionAfterInsert);
				tableBuilder.HasTrigger(SqliteTriggerNames.DevicesEntityAuthInfoVersionAfterAuthUpdate);
				tableBuilder.HasTrigger(SqliteTriggerNames.DevicesEntityAuthInfoVersionAfterDelete);
				tableBuilder.HasCheckConstraint(
					"CK_Devices_DeviceName_IdentityFormat",
					"""
					"DeviceName" GLOB '[A-Za-z0-9]*'
					AND "DeviceName" GLOB '*[A-Za-z0-9]'
					AND length("DeviceName") BETWEEN 4 AND 64
					AND "DeviceName" NOT GLOB '*[^A-Za-z0-9_-]*'
					""");
				tableBuilder.HasCheckConstraint(
					"CK_Devices_NormalizedDeviceName_IdentityFormat",
					"""
					"NormalizedDeviceName" GLOB '[A-Z0-9]*'
					AND "NormalizedDeviceName" GLOB '*[A-Z0-9]'
					AND length("NormalizedDeviceName") BETWEEN 4 AND 64
					AND "NormalizedDeviceName" NOT GLOB '*[^A-Z0-9_-]*'
					""");
				tableBuilder.HasCheckConstraint(
					"CK_Devices_LatestReportedAddresses_JsonArrayShape",
					"""
					"LatestReportedAddresses" IS NULL
					OR (
						json_valid("LatestReportedAddresses")
						AND json_type("LatestReportedAddresses") = 'array'
						AND json_array_length("LatestReportedAddresses") > 0
					)
					""");
				tableBuilder.HasCheckConstraint(
					"CK_Devices_DisplayName_Format",
					OptionalDisplayNameCheckSql);
				tableBuilder.UseSqlReturningClause(false);
			});

			// 设备最新日志摘要主要由 SQLite 触发器回写；SQLite DateTime 文本不保留 Kind，读出时统一补为 UTC
			deviceBuilder.Property(e => e.LatestLogTime)
				.HasConversion(
					value => value.NormalizeToUtcForSQLite(),
					value => value.NormalizeToUtcForSQLite());

			// 定义设备标准化名称为设备实体的人类管理名称唯一索引
			deviceBuilder.HasIndex(e => e.NormalizedDeviceName).IsUnique();

			// 定义设备最新日志时间与标准化名称的复合索引，以优化查询最近活跃设备列表的性能
			deviceBuilder.HasIndex(e => new { e.LatestLogTime, e.NormalizedDeviceName }).IsDescending(true, false);

			// DeviceUser 保留现有表结构，同时显式建模中间表，便于管理侧直接按关系表批量查询和写入
			deviceBuilder
				.HasMany(device => device.AuthorizedUsers)
				.WithMany(user => user.AuthorizedDevices)
				.UsingEntity<DeviceUser>(
					joinBuilder => joinBuilder
						.HasOne(join => join.AuthorizedUser)
						.WithMany(user => user.AuthorizedDeviceLinks)
						.HasForeignKey(join => join.AuthorizedUsersId)
						.OnDelete(DeleteBehavior.Cascade)
						.IsRequired(),
					joinBuilder => joinBuilder
						.HasOne(join => join.AuthorizedDevice)
						.WithMany(device => device.AuthorizedUserLinks)
						.HasForeignKey(join => join.AuthorizedDevicesDeviceId)
						.OnDelete(DeleteBehavior.Cascade)
						.IsRequired(),
					joinBuilder => {
						joinBuilder.HasKey(join => new { join.AuthorizedDevicesDeviceId, join.AuthorizedUsersId });
						joinBuilder.HasIndex(join => join.AuthorizedUsersId);
						joinBuilder.ToTable("DeviceUser");
					});
		});

		// 配置 OnlineLog 实体：声明摘要同步触发器，并为历史查询与摘要回写建立索引
		modelBuilder.Entity<OnlineLog>(onlineLogBuilder => {
			// OnlineLog 表存在 SQLite AFTER 触发器，因此关闭 RETURNING 子句以兼容触发器执行
			onlineLogBuilder.ToTable(tableBuilder => {
				tableBuilder.HasTrigger(SqliteTriggerNames.OnlineLogsDeviceSummaryAfterInsert);
				tableBuilder.HasTrigger(SqliteTriggerNames.OnlineLogsDeviceSummaryAfterUpdate);
				tableBuilder.HasTrigger(SqliteTriggerNames.OnlineLogsDeviceSummaryAfterDelete);
				tableBuilder.HasCheckConstraint(
					"CK_OnlineLogs_ReportedAddresses_JsonArrayShape",
					"""
					json_valid("ReportedAddresses")
					AND json_type("ReportedAddresses") = 'array'
					AND json_array_length("ReportedAddresses") > 0
					""");
				tableBuilder.UseSqlReturningClause(false);
			});

			// 日志时间统一按 UTC 落库；SQLite DateTime 文本不保留 Kind，读出时统一补为 UTC
			onlineLogBuilder.Property(e => e.LogTime)
				.HasConversion(
					value => value.NormalizeToUtcForSQLite(),
					value => value.NormalizeToUtcForSQLite());

			// 为日志时间创建索引以优化查询性能并方便过期数据的删除
			onlineLogBuilder.HasIndex(e => e.LogTime);

			// 为日志创建 (设备 ID, 日志时间) 复合索引以优化按设备查询日志的性能
			onlineLogBuilder.HasIndex(e => new { e.DeviceId, e.LogTime });

			// 设备摘要触发器按 OnlineLogId 倒序寻找“最后到达数据库的一条日志”
			// 因此额外为 (设备 ID, 日志 ID) 建立复合索引以降低回写成本
			onlineLogBuilder.HasIndex(e => new { e.DeviceId, e.OnlineLogId });

			// 非设备主体代提交日志时记录所属用户，便于后续审计
			onlineLogBuilder
				.HasOne(log => log.SubmittedByUser)
				.WithMany(user => user.SubmittedOnlineLogs)
				.HasForeignKey(log => log.SubmittedByUserId)
				.OnDelete(DeleteBehavior.SetNull);
		});

		// 配置 ApiCredential 实体：声明触发器、索引，以及凭据到设备的授权关系
		modelBuilder.Entity<ApiCredential>(apiCredentialBuilder => {
			// ApiCredential 表存在 SQLite AFTER 触发器，因此关闭 RETURNING 子句以兼容触发器执行
			apiCredentialBuilder.ToTable(tableBuilder => {
				tableBuilder.HasTrigger(SqliteTriggerNames.ApiCredentialsEntityAuthInfoVersionAfterInsert);
				tableBuilder.HasTrigger(SqliteTriggerNames.ApiCredentialsEntityAuthInfoVersionAfterUpdate);
				tableBuilder.HasTrigger(SqliteTriggerNames.ApiCredentialsEntityAuthInfoVersionAfterDelete);
				tableBuilder.HasCheckConstraint(
					"CK_ApiCredentials_Role_PrincipalRole",
					"""
					"Role" BETWEEN 0 AND 3
					""");
				tableBuilder.HasCheckConstraint(
					"CK_ApiCredentials_DisplayName_Format",
					RequiredDisplayNameCheckSql);
				tableBuilder.UseSqlReturningClause(false);
			});

			// 定义 API 凭据的 DisplayName 在同一用户下唯一的索引，以方便管理和查询
			// 此索引可同时作用于按 UserId 单项的查询，同样可以提升按 UserId 查询的性能
			apiCredentialBuilder.HasIndex(e => new { e.UserId, e.DisplayName }).IsUnique();

			// ApiCredentialDevice 需要挂接触发器元数据，同时显式建模中间表，便于后续直接操作凭据授权关系
			apiCredentialBuilder
				.HasMany(credential => credential.AuthorizedDevices)
				.WithMany(device => device.AuthorizedApiCredentials)
				.UsingEntity<ApiCredentialDevice>(
					joinBuilder => joinBuilder
						.HasOne(join => join.AuthorizedDevice)
						.WithMany(device => device.AuthorizedApiCredentialLinks)
						.HasForeignKey(join => join.AuthorizedDevicesDeviceId)
						.OnDelete(DeleteBehavior.Cascade)
						.IsRequired(),
					joinBuilder => joinBuilder
						.HasOne(join => join.AuthorizedApiCredential)
						.WithMany(credential => credential.AuthorizedDeviceLinks)
						.HasForeignKey(join => join.AuthorizedApiCredentialsApiCredentialId)
						.OnDelete(DeleteBehavior.Cascade)
						.IsRequired(),
					joinBuilder => {
						joinBuilder.HasKey(join => new { join.AuthorizedApiCredentialsApiCredentialId, join.AuthorizedDevicesDeviceId });
						joinBuilder.HasIndex(join => join.AuthorizedDevicesDeviceId);
						joinBuilder.ToTable("ApiCredentialDevice", tableBuilder => {
							tableBuilder.HasTrigger(SqliteTriggerNames.ApiCredentialDeviceEntityAuthInfoVersionAfterInsert);
							tableBuilder.HasTrigger(SqliteTriggerNames.ApiCredentialDeviceEntityAuthInfoVersionAfterUpdate);
							tableBuilder.HasTrigger(SqliteTriggerNames.ApiCredentialDeviceEntityAuthInfoVersionAfterDelete);
							tableBuilder.UseSqlReturningClause(false);
						});
					});
		});

		// 配置 UserRole 实体：把 Identity 的用户角色关系收紧为“一用户最多一个角色”
		modelBuilder.Entity<UserRole>(userRoleBuilder => {
			// 每个用户最多只能关联一个 Identity 角色，以匹配当前的四层权限模型
			userRoleBuilder.HasIndex(e => e.UserId).IsUnique();

			// 显式配置 UserRole 实体与 IdentityRole 实体之间的关系，确保外键约束和删除行为正确
			userRoleBuilder
				.HasOne(ur => ur.Role)
				.WithMany()
				.HasForeignKey(ur => ur.RoleId)
				.IsRequired()
				.OnDelete(DeleteBehavior.Restrict);

			// 显式配置 UserRole 实体与 User 实体之间的关系，确保外键约束和删除行为正确
			userRoleBuilder
				.HasOne(ur => ur.User)
				.WithMany(u => u.UserRoles)
				.HasForeignKey(ur => ur.UserId)
				.IsRequired()
				.OnDelete(DeleteBehavior.Cascade);
		});

		// 配置 User 实体：为可选显示名称增加数据库层面的基础格式兜底
		modelBuilder.Entity<User>(userBuilder => userBuilder.ToTable(tableBuilder =>
			tableBuilder.HasCheckConstraint(
				"CK_AspNetUsers_DisplayName_Format",
				OptionalDisplayNameCheckSql)));

		// 预置鉴权缓存版本号设置项，供后续触发器自动刷新
		modelBuilder.Entity<SettingInDb>(settingBuilder => {
			settingBuilder.ToTable(tableBuilder =>
				tableBuilder.HasCheckConstraint(
					"CK_Settings_Key_NotEmpty",
					"""length("Key") > 0"""));

			settingBuilder.HasData(new SettingInDb {
				Key = SettingInDbKey.EntityAuthInfoVersion.ToString(),
				Value = "0"
			});
		});

		// 预置固定的 Identity 角色，交由 EF Core Migration 管理
		modelBuilder.Entity<IdentityRole<Guid>>().HasData([
			BuildIdentityRole("5121009f-b5bd-4ec7-95ee-edb11bca4f92", PrincipalRole.LimitedQuery),
			BuildIdentityRole("0e132786-0e18-4cd1-bf41-cfdd18b12d90", PrincipalRole.FullQuery),
			BuildIdentityRole("a8a0d700-c15d-487a-97c6-3359113d367f", PrincipalRole.DeviceManager),
			BuildIdentityRole("3df54d97-ee56-47f1-a1c0-3044dbdb8e41", PrincipalRole.Administrator)
		]);
	}

	/// <summary>
	/// 从配置中获取或构建数据库连接字符串
	/// </summary>
	/// <remarks>当配置了自定义连接字符串时，直接返回该字符串，且返回值中的目录信息将为空；当未配置自定义连接字符串时，基于配置按一定规则构建连接字符串，同时返回数据库目录信息</remarks>
	/// <param name="configuration">传入的配置</param>
	/// <returns>连接字符串和数据库目录信息组成的元组</returns>
	public static (string, DirectoryInfo?) GetOrBuildConnectionStringFromConfiguration(IConfiguration configuration) {
		var dbSection = configuration.GetSection("Database");

		// 尝试直接获取自定义连接字符串
		var dbConnectionString = dbSection["CustomConnectionString"];
		DirectoryInfo? dbDirectoryInfo = null;

		if (string.IsNullOrWhiteSpace(dbConnectionString)) {
			// 尝试从配置中获取数据库目录
			var dbDirectory = dbSection["Directory"];
			if (string.IsNullOrWhiteSpace(dbDirectory)) {
				// 根据配置决定直接使用应用程序基目录下的 data 目录还是按三项规则逐个尝试
				if (dbSection.GetValue("DisableDiscovery", false)) {
					dbDirectory = Path.Join(AppContext.BaseDirectory, "data");
				} else {
					// 尝试使用 LocalApplicationData/DeviceStatusBeacon 目录
					var baseDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
					var subDirectoryName = "DeviceStatusBeacon";
					if (string.IsNullOrWhiteSpace(baseDataPath)) {
						// 尝试使用 UserProfile/.device_status_beacon 目录
						baseDataPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
						subDirectoryName = ".device_status_beacon";
						if (string.IsNullOrWhiteSpace(baseDataPath)) {
							// 使用应用程序基目录下的 data 目录
							baseDataPath = AppContext.BaseDirectory;
							subDirectoryName = "data";
						}
					}
					dbDirectory = Path.Join(baseDataPath, subDirectoryName);
				}
			}

			dbDirectoryInfo = new DirectoryInfo(dbDirectory);

			dbConnectionString = new SqliteConnectionStringBuilder {
				DataSource = Path.Join(dbDirectoryInfo.FullName, dbSection["FileName"] ?? "beacon.db")
			}.ToString();
		}

		return (dbConnectionString, dbDirectoryInfo);
	}

	private const string OptionalDisplayNameCheckSql =
		"""
		"DisplayName" IS NULL
		OR (
			length("DisplayName") BETWEEN 1 AND 64
			AND "DisplayName" = trim("DisplayName")
		)
		""";

	private const string RequiredDisplayNameCheckSql =
		"""
		length("DisplayName") BETWEEN 1 AND 64
		AND "DisplayName" = trim("DisplayName")
		""";

	private static IdentityRole<Guid> BuildIdentityRole(string id, PrincipalRole role) {
		var roleName = role.ToString();
		return new() {
			Id = Guid.Parse(id),
			Name = roleName,
			NormalizedName = roleName.ToUpperInvariant(),
			ConcurrencyStamp = id
		};
	}
}