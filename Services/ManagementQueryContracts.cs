namespace DeviceStatusBeacon.Services;

/// <summary>
/// 表示一次可复用的管理查询会话。
/// </summary>
/// <remarks>
/// Web 页面会基于当前登录主体构建受权限约束的查询会话；
/// CLI 则可以显式创建一个拥有全部查询能力的特权会话。
/// 后续的设备和日志查询都围绕这份会话展开，从而避免重复散落权限判断。
/// 会话本身只保留角色这一份源数据；
/// 读取与管理能力都通过角色扩展方法按需推导，避免在记录中重复存储角色投影。
/// </remarks>
/// <param name="UserId">用户 ID</param>
/// <param name="UserName">用户名或会话显示名</param>
/// <param name="DisplayName">显示名称</param>
/// <param name="Role">当前主体绑定的全局角色；具体读取与管理能力应通过角色扩展方法推导</param>
public sealed record ManagementQuerySession(
	Guid? UserId,
	string UserName,
	string? DisplayName,
	PrincipalRole? Role
) {
	/// <summary>
	/// 转换为页面展示层使用的管理会话数据。
	/// </summary>
	/// <returns>一个供展示层 DTO 使用的管理会话数据</returns>
	public ManagementSessionData ToData() => new(
		UserName,
		DisplayName,
		Role);
}

/// <summary>
/// 管理端当前会话数据。
/// </summary>
/// <remarks>
/// 该记录用于页面展示层，是 <see cref="ManagementQuerySession"/> 的只读快照。
/// 页面如果需要判断查询范围或管理能力，应通过 <see cref="Role"/> 的扩展方法即时推导。
/// </remarks>
/// <param name="UserName">用户名</param>
/// <param name="DisplayName">显示名称</param>
/// <param name="Role">当前主体绑定的全局角色；具体读取与管理能力应通过角色扩展方法推导</param>
public sealed record ManagementSessionData(
	string UserName,
	string? DisplayName,
	PrincipalRole? Role
);

/// <summary>
/// 设备排序方式。
/// </summary>
public enum DeviceSortMode {
	/// <summary>
	/// 按最近日志时间倒序，再按设备名称升序。
	/// </summary>
	RecentActivityDescending,

	/// <summary>
	/// 按设备名称升序。
	/// </summary>
	DeviceNameAscending
}

/// <summary>
/// 设备查询选项。
/// </summary>
/// <param name="SearchTerm">筛选关键字</param>
/// <param name="Take">返回条数</param>
/// <param name="SortMode">排序方式</param>
public sealed record DeviceQueryOptions(
	string? SearchTerm,
	int Take,
	DeviceSortMode SortMode
);

/// <summary>
/// 日志查询选项。
/// </summary>
/// <param name="DeviceKeyword">设备筛选关键字</param>
/// <param name="Take">返回条数</param>
public sealed record LogQueryOptions(
	string? DeviceKeyword,
	int Take
);

/// <summary>
/// Dashboard 首屏摘要数据。
/// </summary>
/// <param name="Session">当前会话信息</param>
/// <param name="AccessibleDeviceCount">当前查询会话范围内可读取的设备总数</param>
/// <param name="EnabledDeviceCount">当前查询会话范围内可读取且启用的设备数</param>
/// <param name="TotalLogCount">当前查询会话范围内可读取的日志总数</param>
public sealed record DashboardOverviewData(
	ManagementSessionData Session,
	int AccessibleDeviceCount,
	int EnabledDeviceCount,
	int TotalLogCount
);

/// <summary>
/// Dashboard 中按需加载的最近活动数据。
/// </summary>
/// <param name="RecentDevices">最近活跃设备摘要</param>
/// <param name="RecentLogs">最近日志摘要</param>
public sealed record DashboardActivityData(
	IReadOnlyCollection<DeviceSummary> RecentDevices,
	IReadOnlyCollection<OnlineLogSummary> RecentLogs
);

/// <summary>
/// 设备列表页数据。
/// </summary>
/// <param name="Session">当前会话信息</param>
/// <param name="TotalCount">当前筛选条件下匹配到的设备总数</param>
/// <param name="Devices">设备列表</param>
public sealed record DeviceListData(
	ManagementSessionData Session,
	int TotalCount,
	IReadOnlyCollection<DeviceSummary> Devices
);

/// <summary>
/// 日志列表页数据。
/// </summary>
/// <param name="Session">当前会话信息</param>
/// <param name="TotalCount">当前筛选条件下匹配到的日志总数</param>
/// <param name="Logs">日志列表</param>
public sealed record LogListData(
	ManagementSessionData Session,
	int TotalCount,
	IReadOnlyCollection<OnlineLogSummary> Logs
);

/// <summary>
/// 设备摘要。
/// </summary>
/// <param name="DeviceId">设备 ID</param>
/// <param name="DeviceName">设备名称</param>
/// <param name="DisplayName">设备显示名称</param>
/// <param name="Enabled">是否启用</param>
/// <param name="LatestLogTime">最近日志时间</param>
/// <param name="LatestReportedAddresses">最近上报地址</param>
/// <param name="LatestReporterRemoteAddress">最近上报来源地址</param>
public sealed record DeviceSummary(
	Guid DeviceId,
	string DeviceName,
	string? DisplayName,
	bool Enabled,
	DateTime? LatestLogTime,
	IReadOnlyCollection<string> LatestReportedAddresses,
	string? LatestReporterRemoteAddress
);

/// <summary>
/// 日志摘要。
/// </summary>
/// <param name="OnlineLogId">日志 ID</param>
/// <param name="DeviceId">设备 ID</param>
/// <param name="DeviceName">设备名称</param>
/// <param name="DeviceDisplayName">设备显示名称</param>
/// <param name="LogTime">日志时间</param>
/// <param name="ReportedAddresses">上报地址</param>
/// <param name="ReporterRemoteAddress">上报来源地址</param>
/// <param name="Message">附加消息</param>
public sealed record OnlineLogSummary(
	long OnlineLogId,
	Guid DeviceId,
	string DeviceName,
	string? DeviceDisplayName,
	DateTime LogTime,
	IReadOnlyCollection<string> ReportedAddresses,
	string? ReporterRemoteAddress,
	string? Message
);