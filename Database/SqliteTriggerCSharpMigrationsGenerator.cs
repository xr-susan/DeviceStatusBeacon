using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace DeviceStatusBeacon.Database;

/// <summary>
/// 标记“创建 EntityAuthInfoVersion 触发器集”的迁移操作
/// </summary>
internal sealed class CreateEntityAuthInfoVersionTriggersOperation : MigrationOperation;

/// <summary>
/// 标记“创建 OnlineLog 设备摘要触发器集”的迁移操作
/// </summary>
internal sealed class CreateOnlineLogDeviceSummaryTriggersOperation : MigrationOperation;

/// <summary>
/// 标记“删除本项目统一管理的全部 SQLite 触发器”的迁移操作
/// </summary>
internal sealed class DropManagedSqliteTriggersOperation : MigrationOperation;

/// <summary>
/// 用于在 EF Core 生成迁移代码时自动补入本项目触发器调用的迁移代码生成器
/// </summary>
/// <remarks>
/// EF Core 自身不会根据 <c>HasTrigger</c> 或模型配置自动脚手架出 SQLite 的 <c>CREATE TRIGGER</c> SQL。
/// 因此这里接管迁移操作列表，在识别到“初始化整库结构”的迁移时自动补入自定义触发器调用，
/// 再交由专门的 <see cref="ICSharpMigrationOperationGenerator"/> 生成稳定的 C# 调用。
/// </remarks>
internal sealed class SqliteTriggerCSharpMigrationsGenerator(
	MigrationsCodeGeneratorDependencies dependencies,
	CSharpMigrationsGeneratorDependencies cSharpDependencies)
	: CSharpMigrationsGenerator(dependencies, cSharpDependencies) {
	/// <summary>
	/// 用于识别需要自动补入触发器调用的核心表集合
	/// </summary>
	/// <remarks>
	/// 当前策略不是按迁移名称判断，而是按是否同时创建并回滚这组核心业务表判断。
	/// 这样即使迁移名称变化，或者初始迁移重新脚手架为新的时间戳，也仍能自动注入触发器调用。
	/// </remarks>
	private static readonly string[] TriggerDependentTableNames = [
		"Devices",
		"OnlineLogs",
		"ApiCredentials",
		"Settings"
	];

	// 当前 DeviceStatusBeacon.Database 命名空间已经在项目级别 using 列表中，这里不需要再次添加 using 语句
	// private static readonly string TriggerMigrationExtensionsNamespace = typeof(SqliteTriggerMigrationBuilderExtensions).Namespace!;

	/// <summary>
	/// 生成迁移代码，并在识别到初始结构迁移时自动补入触发器调用
	/// </summary>
	/// <remarks>
	/// 普通增量迁移直接保持 EF Core 默认输出，只有识别为初始整库迁移时才会向操作列表追加触发器调用。
	/// 这样可以避免把触发器调用误插入到后续只改某一个表或索引的迁移中。
	/// </remarks>
	/// <param name="migrationNamespace">迁移命名空间</param>
	/// <param name="migrationName">迁移名称</param>
	/// <param name="upOperations">Up 方法中的迁移操作</param>
	/// <param name="downOperations">Down 方法中的迁移操作</param>
	/// <returns>生成的 C# 迁移源码</returns>
	public override string GenerateMigration(
		string? migrationNamespace,
		string migrationName,
		IReadOnlyList<MigrationOperation> upOperations,
		IReadOnlyList<MigrationOperation> downOperations) {

		// 只有当迁移同时创建和回滚了 TriggerDependentTableNames 中的全部核心表时，才注入触发器调用
		// 这样可以以极高的可靠性识别出“初始化整库结构”的迁移，而不受迁移名称或时间戳变化的影响，同时避免把触发器调用误插入到普通增量迁移中
		if (!ShouldInjectManagedTriggerOperations(upOperations, downOperations)) {
			return base.GenerateMigration(migrationNamespace, migrationName, upOperations, downOperations);
		}

		var upOperationsWithManagedTriggerCalls = upOperations
			.Concat([
				new CreateEntityAuthInfoVersionTriggersOperation(),
				new CreateOnlineLogDeviceSummaryTriggersOperation()
			])
			.ToArray();

		var downOperationsWithManagedTriggerDrop = (MigrationOperation[])[
			new DropManagedSqliteTriggersOperation(),
			.. downOperations
		];

		return base.GenerateMigration(
			migrationNamespace,
			migrationName,
			upOperationsWithManagedTriggerCalls,
			downOperationsWithManagedTriggerDrop);
	}

	/* 当前 DeviceStatusBeacon.Database 命名空间已经在项目级别 using 列表中，这里不需要再次添加 using 语句
	/// <inheritdoc />
	protected override IEnumerable<string> GetNamespaces(IEnumerable<MigrationOperation> operations) {
		var operationArray = operations as MigrationOperation[] ?? [.. operations];
		var namespaces = base.GetNamespaces(operationArray).ToHashSet(StringComparer.Ordinal);

		// 如果迁移操作列表中包含本项目的触发器相关迁移操作，则补入触发器迁移扩展方法所在的命名空间以确保生成代码能正确编译
		if (operationArray.Any(static operation =>
				operation is CreateEntityAuthInfoVersionTriggersOperation
				or CreateOnlineLogDeviceSummaryTriggersOperation
				or DropManagedSqliteTriggersOperation)) {
			namespaces.Add(TriggerMigrationExtensionsNamespace);
		}

		return namespaces.OrderBy(static namespaceName => namespaceName, StringComparer.Ordinal);
	}
	*/

	/// <summary>
	/// 判断当前脚手架出来的迁移是否应自动补入受管触发器调用
	/// </summary>
	/// <remarks>
	/// 这里按核心表集合判断，从而避免将触发器调用错误注入到普通增量迁移中。
	/// </remarks>
	private static bool ShouldInjectManagedTriggerOperations(
		IReadOnlyList<MigrationOperation> upOperations,
		IReadOnlyList<MigrationOperation> downOperations) {
		var createdTableNames = upOperations
			.OfType<CreateTableOperation>()
			.Select(operation => operation.Name)
			.ToHashSet(StringComparer.Ordinal);

		var droppedTableNames = downOperations
			.OfType<DropTableOperation>()
			.Select(operation => operation.Name)
			.ToHashSet(StringComparer.Ordinal);

		return createdTableNames.IsSupersetOf(TriggerDependentTableNames)
			&& droppedTableNames.IsSupersetOf(TriggerDependentTableNames);
	}
}

/// <summary>
/// 为本项目的自定义触发器调用生成稳定的 C# 代码
/// </summary>
internal sealed class SqliteTriggerCSharpMigrationOperationGenerator(CSharpMigrationOperationGeneratorDependencies dependencies)
	: CSharpMigrationOperationGenerator(dependencies) {
	/// <inheritdoc />
	protected override void Generate(MigrationOperation operation, IndentedStringBuilder builder) {

		// EF Core 外层会负责写入 "migrationBuilder" 前缀和结尾分号
		// 这里仅输出附着在 MigrationBuilder 实例后的调用片段
		switch (operation) {
			case CreateEntityAuthInfoVersionTriggersOperation:
				builder.Append(".CreateEntityAuthInfoVersionTriggers()");
				return;

			case CreateOnlineLogDeviceSummaryTriggersOperation:
				builder.Append(".CreateOnlineLogDeviceSummaryTriggers()");
				return;

			case DropManagedSqliteTriggersOperation:
				builder.Append(".DropManagedSqliteTriggers()");
				return;
		}

		base.Generate(operation, builder);
	}
}