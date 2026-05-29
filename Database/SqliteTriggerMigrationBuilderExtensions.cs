using Microsoft.EntityFrameworkCore.Migrations;

namespace DeviceStatusBeacon.Database;

/// <summary>
/// 用于向 <see cref="MigrationBuilder"/> 注入本项目所需 SQLite 触发器定义的扩展方法组
/// </summary>
/// <remarks>
/// 这些触发器的 SQL 定义集中放在数据库层辅助代码中，由 EF Core 迁移统一调用。
/// 这样做可以避免在迁移类中散落大段 SQL，同时也降低重建初始迁移时遗漏触发器定义的概率。
/// </remarks>
internal static class SqliteTriggerMigrationBuilderExtensions {
	extension(MigrationBuilder migrationBuilder) {
		/// <summary>
		/// 创建所有用于刷新 <c>EntityAuthInfoVersion</c> 的 SQLite 触发器
		/// </summary>
		/// <remarks>
		/// 当前只对自定义签名鉴权直接依赖的实体刷新版本号：
		/// <c>Device</c>、<c>ApiCredential</c> 及其直接授权关系。
		/// 浏览器侧 Identity 用户体系不参与这组触发器，以避免不必要的模型和迁移复杂度。
		/// </remarks>
		public void CreateEntityAuthInfoVersionTriggers() {

			// 这些触发器负责在鉴权相关实体被写入后刷新版本字符串，
			// 让运行中的进程可以据此判断本地鉴权缓存是否应当失效。
			EmitSimpleVersionTriggers(
				migrationBuilder,
				"ApiCredentials",
				SqliteTriggerNames.ApiCredentialsEntityAuthInfoVersionAfterInsert,
				SqliteTriggerNames.ApiCredentialsEntityAuthInfoVersionAfterUpdate,
				SqliteTriggerNames.ApiCredentialsEntityAuthInfoVersionAfterDelete,
				UpdateEntityAuthInfoVersionSql);

			EmitSimpleVersionTriggers(
				migrationBuilder,
				"ApiCredentialDevice",
				SqliteTriggerNames.ApiCredentialDeviceEntityAuthInfoVersionAfterInsert,
				SqliteTriggerNames.ApiCredentialDeviceEntityAuthInfoVersionAfterUpdate,
				SqliteTriggerNames.ApiCredentialDeviceEntityAuthInfoVersionAfterDelete,
				UpdateEntityAuthInfoVersionSql);

			migrationBuilder.Sql($$"""
				CREATE TRIGGER "{{SqliteTriggerNames.DevicesEntityAuthInfoVersionAfterInsert}}"
				AFTER INSERT ON "Devices"
				BEGIN
				{{UpdateEntityAuthInfoVersionSql}}
				END;

				CREATE TRIGGER "{{SqliteTriggerNames.DevicesEntityAuthInfoVersionAfterAuthUpdate}}"
				AFTER UPDATE OF "DeviceName", "ProtectedSecretKey", "DisplayName", "Enabled" ON "Devices"
				BEGIN
				{{UpdateEntityAuthInfoVersionSql}}
				END;

				CREATE TRIGGER "{{SqliteTriggerNames.DevicesEntityAuthInfoVersionAfterDelete}}"
				AFTER DELETE ON "Devices"
				BEGIN
				{{UpdateEntityAuthInfoVersionSql}}
				END;
				""");
		}

		/// <summary>
		/// 创建所有用于同步 <see cref="Device"/> 最新日志摘要的 SQLite 触发器
		/// </summary>
		/// <remarks>
		/// 应用层写入日志时只需要维护 <c>OnlineLogs</c>，设备上的最新摘要由数据库统一回写。
		/// 这样可以避免不同写入入口各自执行第二次 <c>Device</c> 更新，减少规则分散和漏同步风险。
		/// </remarks>
		public void CreateOnlineLogDeviceSummaryTriggers() {
			var refreshOldDeviceSummarySql = BuildRefreshDeviceSummarySql(@"OLD.""DeviceId""");
			var refreshNewDeviceSummarySql = BuildRefreshDeviceSummarySql(@"NEW.""DeviceId""");

			migrationBuilder.Sql($$"""
				CREATE TRIGGER "{{SqliteTriggerNames.OnlineLogsDeviceSummaryAfterInsert}}"
				AFTER INSERT ON "OnlineLogs"
				BEGIN
				{{refreshNewDeviceSummarySql}}
				END;

				CREATE TRIGGER "{{SqliteTriggerNames.OnlineLogsDeviceSummaryAfterUpdate}}"
				AFTER UPDATE ON "OnlineLogs"
				BEGIN
				{{refreshOldDeviceSummarySql}}
				{{refreshNewDeviceSummarySql}}
				END;

				CREATE TRIGGER "{{SqliteTriggerNames.OnlineLogsDeviceSummaryAfterDelete}}"
				AFTER DELETE ON "OnlineLogs"
				BEGIN
				{{refreshOldDeviceSummarySql}}
				END;
				""");
		}

		/// <summary>
		/// 删除本项目统一管理的全部 SQLite 触发器
		/// </summary>
		/// <remarks>
		/// 迁移回滚时先统一删除触发器，再删除表结构，避免回滚顺序受数据库对象依赖影响。
		/// </remarks>
		public void DropManagedSqliteTriggers() {
			foreach (var triggerName in SqliteTriggerNames.ManagedTriggerNames) {
				migrationBuilder.Sql($$"""DROP TRIGGER IF EXISTS "{{triggerName}}";""");
			}
		}
	}

	/// <summary>
	/// 刷新 <c>EntityAuthInfoVersion</c> 的 SQL 片段
	/// </summary>
	/// <remarks>
	/// 版本值本身不承载业务语义，只需要在每次相关写入后变成一个新值即可。
	/// 因此直接使用 SQLite 的 <c>randomblob</c> 生成短随机十六进制字符串。
	/// </remarks>
	private const string UpdateEntityAuthInfoVersionSql = """
		UPDATE "Settings"
		SET "Value" = lower(hex(randomblob(16)))
		WHERE "Key" = 'EntityAuthInfoVersion';
		""";

	/// <summary>
	/// 为“插入、更新、删除均执行同一版本刷新逻辑”的表生成统一的触发器 SQL
	/// </summary>
	private static void EmitSimpleVersionTriggers(
		MigrationBuilder migrationBuilder,
		string tableName,
		string afterInsertTriggerName,
		string afterUpdateTriggerName,
		string afterDeleteTriggerName,
		string bodySql) =>
		migrationBuilder.Sql(BuildSimpleVersionTriggerSql(
			tableName,
			afterInsertTriggerName,
			afterUpdateTriggerName,
			afterDeleteTriggerName,
			bodySql));

	/// <summary>
	/// 构造适用于简单三段式表触发器的 SQL
	/// </summary>
	/// <remarks>
	/// 适用于“插入、更新、删除都执行相同版本刷新逻辑”的表，
	/// 用一个模板统一生成三类触发器，减少重复 SQL 文本。
	/// </remarks>
	/// <param name="tableName">需要挂接触发器的表名</param>
	/// <param name="afterInsertTriggerName">插入触发器名称</param>
	/// <param name="afterUpdateTriggerName">更新触发器名称</param>
	/// <param name="afterDeleteTriggerName">删除触发器名称</param>
	/// <param name="bodySql">触发器主体 SQL</param>
	/// <returns>创建触发器的 SQL 文本</returns>
	private static string BuildSimpleVersionTriggerSql(
		string tableName,
		string afterInsertTriggerName,
		string afterUpdateTriggerName,
		string afterDeleteTriggerName,
		string bodySql) => $$"""
		CREATE TRIGGER "{{afterInsertTriggerName}}"
		AFTER INSERT ON "{{tableName}}"
		BEGIN
		{{bodySql}}
		END;

		CREATE TRIGGER "{{afterUpdateTriggerName}}"
		AFTER UPDATE ON "{{tableName}}"
		BEGIN
		{{bodySql}}
		END;

		CREATE TRIGGER "{{afterDeleteTriggerName}}"
		AFTER DELETE ON "{{tableName}}"
		BEGIN
		{{bodySql}}
		END;
		""";

	/// <summary>
	/// 构造按设备重新计算最新日志摘要的 SQL 片段
	/// </summary>
	/// <remarks>
	/// 这里用一次相关子查询同时返回三列，提升触发器执行效率，避免为同一设备重复执行三次最新日志查找。
	/// “最新”在这里定义为最后到达数据库的一条日志，因此按 <c>OnlineLogId</c> 倒序判定。
	/// 设备表保留这三列摘要冗余，是为了让设备列表和设备详情读取当前状态时保持单表读取成本，
	/// 而不是每次都回表联接 <c>OnlineLogs</c> 再取最新记录。
	/// </remarks>
	/// <param name="deviceIdSql">设备 ID 的 SQL 表达式</param>
	/// <returns>触发器内部执行的 SQL 片段</returns>
	private static string BuildRefreshDeviceSummarySql(string deviceIdSql) => $$"""
		UPDATE "Devices"
		SET (
			"LatestLogTime",
			"LatestReportedAddresses",
			"LatestReporterRemoteAddress"
		) = (
			SELECT
				"LogTime",
				"ReportedAddresses",
				"ReporterRemoteAddress"
			FROM "OnlineLogs"
			WHERE "DeviceId" = {{deviceIdSql}}
			ORDER BY "OnlineLogId" DESC
			LIMIT 1
		)
		WHERE "DeviceId" = {{deviceIdSql}};
		""";
}