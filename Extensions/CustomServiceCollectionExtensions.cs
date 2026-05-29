using DeviceStatusBeacon.Services;
using Microsoft.AspNetCore.DataProtection;

namespace DeviceStatusBeacon.Extensions;

/// <summary>
/// 用于添加本项目的自定义服务和定制化的服务到 <see cref="IServiceCollection"/> 中的扩展方法组
/// </summary>
public static class CustomServiceCollectionExtensions {
	extension(IServiceCollection services) {
		/// <summary>
		/// 注册本项目中自定义的服务到 <see cref="IServiceCollection"/> 中
		/// </summary>
		/// <param name="services">将要添加服务的 <see cref="IServiceCollection"/></param>
		/// <returns>当前的 <see cref="IServiceCollection"/>，用于链式调用</returns>
		public IServiceCollection AddCustomServices() =>
			services.AddSingleton<IDataProtectorV1, DataProtectorV1>()
				.AddScoped<ISecurityServiceV1, SecurityServiceV1>()
				.AddScoped<IBackOfficeQueryService, BackOfficeQueryService>();

		/// <summary>
		/// 依据配置注册数据库上下文 <see cref="DeviceStatusBeaconContext"/> 到 <see cref="IServiceCollection"/> 中
		/// </summary>
		/// <remarks>数据库连接字符串会自动根据配置进行构建</remarks>
		/// <param name="configuration">传入的配置</param>
		/// <param name="dbDirectoryInfo">从配置中解析的数据库目录信息，如果使用了自定义连接字符串，此项将为 null</param>
		/// <returns>当前的 <see cref="IServiceCollection"/>，用于链式调用</returns>
		/// <exception cref="IOException">当创建数据库目录失败时</exception>
		public IServiceCollection AddDbContextsWithConfiguration(IConfiguration configuration, out DirectoryInfo? dbDirectoryInfo) {
			string? dbConnectionString = null;

			// 获取或构建数据库连接字符串
			(dbConnectionString, dbDirectoryInfo) = DeviceStatusBeaconContext.GetOrBuildConnectionStringFromConfiguration(configuration);

			// 确保数据库目录存在
			dbDirectoryInfo?.Create();

			// 注册 DbContext 池
			services.AddDbContextPool<DeviceStatusBeaconContext>(options => options.UseSqlite(dbConnectionString));

			return services;
		}

		/// <summary>
		/// 依据配置注册数据保护 API 到 <see cref="IServiceCollection"/> 中
		/// </summary>
		/// <param name="configuration">传入的配置</param>
		/// <param name="dbDirectoryInfo">数据库目录信息</param>
		/// <returns>当前的 <see cref="IServiceCollection"/>，用于链式调用</returns>
		/// <exception cref="InvalidOperationException">当未指定数据保护密钥目录的父目录且数据库目录不可用时</exception>
		/// <exception cref="IOException">当创建数据保护密钥目录失败时</exception>
		public IServiceCollection AddDataProtectionWithConfiguration(IConfiguration configuration, DirectoryInfo? dbDirectoryInfo) {
			// 配置数据保护 API
			var dpBuilder = services.AddDataProtection().SetApplicationName("DeviceStatusBeacon");

			// 配置数据保护密钥存储目录
			// 如果配置中显式禁用了覆盖发现，则不进行任何操作，使用数据保护 API 的默认行为
			if (configuration.GetValue("DataProtection:OverrideDiscovery", true)) {
				// 尝试从配置中获取数据保护密钥目录的父目录
				var dpKeysDirectoryParent = configuration["DataProtection:KeysDirectoryParent"];

				// 如果配置中未指定数据保护密钥目录的父目录，则使用数据库目录作为父目录
				var dpKeysDirectoryParentInfo = (string.IsNullOrWhiteSpace(dpKeysDirectoryParent) ? dbDirectoryInfo : new DirectoryInfo(dpKeysDirectoryParent))
					?? throw new InvalidOperationException("Data protection keys directory parent is not specified and database directory is also unavailable.");

				// 创建数据保护密钥目录并配置数据保护 API 使用该目录存储密钥
				var dpKeysDirectoryInfo = dpKeysDirectoryParentInfo.CreateSubdirectory("DataProtection-Keys");
				dpBuilder.PersistKeysToFileSystem(dpKeysDirectoryInfo);
			}

			return services;
		}
	}

	extension(IServiceProvider serviceProvider) {
		/// <summary>
		/// 应用数据库迁移
		/// </summary>
		/// <returns>一个表示异步迁移操作的任务</returns>
		public async Task MigrateDatabaseAsync() {
			using var scope = serviceProvider.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<DeviceStatusBeaconContext>();
			await dbContext.Database.MigrateAsync();
		}
	}
}