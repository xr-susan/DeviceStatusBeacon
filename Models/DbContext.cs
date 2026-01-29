namespace DeviceStatusBeacon.Models;

public class DeviceStatusBeaconContext : DbContext {
	/// <summary>
	/// 存储在线日志 <seealso cref="OnlineLog"/> 的实体集合，由 EF Core 自动映射到数据库表
	/// </summary>
	public DbSet<OnlineLog> OnlineLogs { get; set; }

	/// <summary>
	/// 存储设备 <seealso cref="Device"/> 的实体集合，由 EF Core 自动映射到数据库表
	/// </summary>
	public DbSet<Device> Devices { get; set; }

	/// <summary>
	/// 存储账户 <seealso cref="Account"/> 的实体集合，由 EF Core 自动映射到数据库表
	/// </summary>
	public DbSet<Account> Accounts { get; set; }

	/// <inheritdoc/>
	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		// 定义设备名称为设备实体的唯一索引
		modelBuilder.Entity<Device>().HasIndex(e => e.DeviceName).IsUnique();

		// 为日志时间创建索引以优化查询性能并方便过期数据的删除
		modelBuilder.Entity<OnlineLog>().HasIndex(e => e.LogTime);

		// 定义用户名为账户实体的唯一索引
		modelBuilder.Entity<Account>().HasIndex(e => e.Username).IsUnique();
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
}