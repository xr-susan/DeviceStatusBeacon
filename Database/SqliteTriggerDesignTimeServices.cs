using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeviceStatusBeacon.Database;

/// <summary>
/// 注册本项目的 EF Core 设计期服务
/// </summary>
/// <remarks>
/// 当前项目会持续重建初始迁移，因此需要在脚手架阶段稳定补入触发器辅助调用。
/// 这里同时替换迁移代码生成器与迁移操作代码生成器，避免再依赖脆弱的源码文本切割。
/// </remarks>
public sealed class SqliteTriggerDesignTimeServices : IDesignTimeServices {
	/// <inheritdoc />
	public void ConfigureDesignTimeServices(IServiceCollection serviceCollection) {
		serviceCollection.Replace(ServiceDescriptor.Singleton<IMigrationsCodeGenerator, SqliteTriggerCSharpMigrationsGenerator>());
		serviceCollection.Replace(ServiceDescriptor.Singleton<ICSharpMigrationOperationGenerator, SqliteTriggerCSharpMigrationOperationGenerator>());
	}
}