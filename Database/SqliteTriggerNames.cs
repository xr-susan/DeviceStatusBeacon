namespace DeviceStatusBeacon.Database;

/// <summary>
/// 当前项目统一使用的 SQLite 触发器名称常量
/// </summary>
internal static class SqliteTriggerNames {
	/// <summary>
	/// <c>OnlineLogs</c> 插入后同步设备最新摘要的触发器名称
	/// </summary>
	internal const string OnlineLogsDeviceSummaryAfterInsert = "TR_OnlineLogs_DeviceSummary_AfterInsert";

	/// <summary>
	/// <c>OnlineLogs</c> 更新后同步设备最新摘要的触发器名称
	/// </summary>
	internal const string OnlineLogsDeviceSummaryAfterUpdate = "TR_OnlineLogs_DeviceSummary_AfterUpdate";

	/// <summary>
	/// <c>OnlineLogs</c> 删除后同步设备最新摘要的触发器名称
	/// </summary>
	internal const string OnlineLogsDeviceSummaryAfterDelete = "TR_OnlineLogs_DeviceSummary_AfterDelete";

	/// <summary>
	/// <c>Devices</c> 插入后刷新 <c>EntityAuthInfoVersion</c> 的触发器名称
	/// </summary>
	internal const string DevicesEntityAuthInfoVersionAfterInsert = "TR_Devices_EntityAuthInfoVersion_AfterInsert";

	/// <summary>
	/// <c>Devices</c> 鉴权相关字段更新后刷新 <c>EntityAuthInfoVersion</c> 的触发器名称
	/// </summary>
	internal const string DevicesEntityAuthInfoVersionAfterAuthUpdate = "TR_Devices_EntityAuthInfoVersion_AfterAuthUpdate";

	/// <summary>
	/// <c>Devices</c> 删除后刷新 <c>EntityAuthInfoVersion</c> 的触发器名称
	/// </summary>
	internal const string DevicesEntityAuthInfoVersionAfterDelete = "TR_Devices_EntityAuthInfoVersion_AfterDelete";

	/// <summary>
	/// <c>ApiCredentialDevice</c> 插入后刷新 <c>EntityAuthInfoVersion</c> 的触发器名称
	/// </summary>
	internal const string ApiCredentialDeviceEntityAuthInfoVersionAfterInsert = "TR_ApiCredentialDevice_EntityAuthInfoVersion_AfterInsert";

	/// <summary>
	/// <c>ApiCredentialDevice</c> 更新后刷新 <c>EntityAuthInfoVersion</c> 的触发器名称
	/// </summary>
	internal const string ApiCredentialDeviceEntityAuthInfoVersionAfterUpdate = "TR_ApiCredentialDevice_EntityAuthInfoVersion_AfterUpdate";

	/// <summary>
	/// <c>ApiCredentialDevice</c> 删除后刷新 <c>EntityAuthInfoVersion</c> 的触发器名称
	/// </summary>
	internal const string ApiCredentialDeviceEntityAuthInfoVersionAfterDelete = "TR_ApiCredentialDevice_EntityAuthInfoVersion_AfterDelete";

	/// <summary>
	/// <c>ApiCredentials</c> 插入后刷新 <c>EntityAuthInfoVersion</c> 的触发器名称
	/// </summary>
	internal const string ApiCredentialsEntityAuthInfoVersionAfterInsert = "TR_ApiCredentials_EntityAuthInfoVersion_AfterInsert";

	/// <summary>
	/// <c>ApiCredentials</c> 更新后刷新 <c>EntityAuthInfoVersion</c> 的触发器名称
	/// </summary>
	internal const string ApiCredentialsEntityAuthInfoVersionAfterUpdate = "TR_ApiCredentials_EntityAuthInfoVersion_AfterUpdate";

	/// <summary>
	/// <c>ApiCredentials</c> 删除后刷新 <c>EntityAuthInfoVersion</c> 的触发器名称
	/// </summary>
	internal const string ApiCredentialsEntityAuthInfoVersionAfterDelete = "TR_ApiCredentials_EntityAuthInfoVersion_AfterDelete";

	/// <summary>
	/// 当前项目统一管理的全部 SQLite 触发器名称列表
	/// </summary>
	internal static readonly string[] ManagedTriggerNames = [
		OnlineLogsDeviceSummaryAfterDelete,
		OnlineLogsDeviceSummaryAfterUpdate,
		OnlineLogsDeviceSummaryAfterInsert,
		DevicesEntityAuthInfoVersionAfterDelete,
		DevicesEntityAuthInfoVersionAfterAuthUpdate,
		DevicesEntityAuthInfoVersionAfterInsert,
		ApiCredentialDeviceEntityAuthInfoVersionAfterDelete,
		ApiCredentialDeviceEntityAuthInfoVersionAfterUpdate,
		ApiCredentialDeviceEntityAuthInfoVersionAfterInsert,
		ApiCredentialsEntityAuthInfoVersionAfterDelete,
		ApiCredentialsEntityAuthInfoVersionAfterUpdate,
		ApiCredentialsEntityAuthInfoVersionAfterInsert
	];
}