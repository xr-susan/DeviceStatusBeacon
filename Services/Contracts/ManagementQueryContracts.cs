using System.Net;

namespace DeviceStatusBeacon.Services.Contracts;

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
/// <param name="PrincipalId">参与授权范围判断的主体 ID；特权查询会话不需要具体主体 ID</param>
/// <param name="PrincipalKind">参与授权范围判断的主体类型</param>
/// <param name="UserName">用户名或会话显示名，仅适用于交互式会话</param>
/// <param name="DisplayName">显示名称，仅适用于交互式会话</param>
/// <param name="Role">当前主体绑定的全局角色；具体读取与管理能力应通过角色扩展方法推导</param>
public sealed record ManagementQuerySession(
	Guid? PrincipalId,
	ManagementQueryPrincipalKind PrincipalKind,
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
/// 管理查询会话的授权主体类型。
/// </summary>
public enum ManagementQueryPrincipalKind {
	/// <summary>
	/// 未识别或不支持管理查询的主体。
	/// </summary>
	Unknown,

	/// <summary>
	/// 交互式后台用户。
	/// </summary>
	User,

	/// <summary>
	/// 签名式 API 凭据。
	/// </summary>
	ApiCredential,

	/// <summary>
	/// 内部特权查询会话。
	/// </summary>
	Privileged
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
/// 分页数据。
/// </summary>
/// <param name="TotalCount">当前筛选条件下匹配到的数据总数</param>
/// <param name="PageNumber">当前页码</param>
/// <param name="PageSize">每页数量</param>
public sealed record PaginationData {
	/// <summary>
	/// 初始化分页数据。
	/// </summary>
	/// <param name="totalCount">当前筛选条件下匹配到的数据总数</param>
	/// <param name="pageNumber">当前页码</param>
	/// <param name="pageSize">每页数量</param>
	public PaginationData(int totalCount, int pageNumber, int pageSize) {
		if (pageSize <= 0) {
			throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "每页数量必须大于 0。");
		}

		TotalCount = totalCount;
		PageNumber = pageNumber;
		PageSize = pageSize;
	}

	/// <summary>
	/// 当前筛选条件下匹配到的数据总数。
	/// </summary>
	public int TotalCount { get; init; }

	/// <summary>
	/// 当前页码。
	/// </summary>
	public int PageNumber { get; init; }

	/// <summary>
	/// 每页数量。
	/// </summary>
	public int PageSize { get; init; }

	/// <summary>
	/// 总页数。
	/// </summary>
	public int TotalPages => GetTotalPages(TotalCount, PageSize);

	/// <summary>
	/// 是否存在上一页。
	/// </summary>
	public bool HasPreviousPage => PageNumber > 1;

	/// <summary>
	/// 是否存在下一页。
	/// </summary>
	public bool HasNextPage => PageNumber < TotalPages;

	/// <summary>
	/// 根据数据总数和每页数量计算总页数。
	/// </summary>
	/// <param name="totalCount">数据总数</param>
	/// <param name="pageSize">每页数量</param>
	/// <returns>总页数</returns>
	public static int GetTotalPages(int totalCount, int pageSize) {
		if (pageSize <= 0) {
			// 对传入的每页数量进行验证，确保其大于 0；如果不合法，则抛出异常
			throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "每页数量必须大于 0。");
		}

		return totalCount == 0 ? 1 : (totalCount + pageSize - 1) / pageSize;
	}
}

/// <summary>
/// Dashboard 首屏摘要数据。
/// </summary>
/// <param name="Session">当前会话信息</param>
/// <param name="AccessibleDeviceCount">当前查询会话范围内可读取的设备总数</param>
/// <param name="EnabledDeviceCount">当前查询会话范围内可读取且启用的设备数</param>
/// <param name="RecentActiveDeviceCount">当前查询会话范围内近期有日志的设备数</param>
/// <param name="RecentActiveWindowHours">近期活动统计使用的时间窗口（小时）</param>
public sealed record DashboardOverviewData(
	ManagementSessionData Session,
	int AccessibleDeviceCount,
	int EnabledDeviceCount,
	int RecentActiveDeviceCount,
	int RecentActiveWindowHours
);

/// <summary>
/// Dashboard 中按需加载的补充摘要与最近活动数据。
/// </summary>
/// <param name="AccessibleLogCount">当前查询会话范围内可读取的日志总数</param>
/// <param name="RecentDeviceActivities">近期活跃设备摘要</param>
public sealed record DashboardActivityData(
	int AccessibleLogCount,
	IReadOnlyCollection<DeviceActivitySummary> RecentDeviceActivities
);

/// <summary>
/// Dashboard 近期设备活动摘要。
/// </summary>
/// <param name="DeviceName">设备名称</param>
/// <param name="DisplayName">设备显示名称</param>
/// <param name="Enabled">是否启用</param>
/// <param name="LatestLogTime">最近日志时间</param>
/// <param name="LatestReportedAddresses">最近上报地址</param>
/// <param name="LatestReporterRemoteAddress">最近上报来源地址</param>
/// <param name="RecentLogCount">近期日志数</param>
public sealed record DeviceActivitySummary(
	string DeviceName,
	string? DisplayName,
	bool Enabled,
	DateTime LatestLogTime,
	IReadOnlyCollection<IPAddress>? LatestReportedAddresses,
	IPAddress? LatestReporterRemoteAddress,
	int RecentLogCount
);

/// <summary>
/// 设备列表页数据。
/// </summary>
/// <param name="Session">当前会话信息</param>
/// <param name="Pagination">分页数据</param>
/// <param name="Devices">设备列表</param>
public sealed record DeviceListData(
	ManagementSessionData Session,
	PaginationData Pagination,
	IReadOnlyCollection<DeviceSummary> Devices
);

/// <summary>
/// 单设备详情数据。
/// </summary>
/// <param name="Device">设备摘要</param>
/// <param name="RecentLogs">最近日志</param>
public sealed record DeviceDetailsData(
	DeviceSummary Device,
	IReadOnlyCollection<OnlineLogSummary> RecentLogs
);

/// <summary>
/// 日志列表页数据。
/// </summary>
/// <param name="Session">当前会话信息</param>
/// <param name="Pagination">分页数据</param>
/// <param name="Logs">日志列表</param>
public sealed record LogListData(
	ManagementSessionData Session,
	PaginationData Pagination,
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
	IReadOnlyCollection<IPAddress>? LatestReportedAddresses,
	IPAddress? LatestReporterRemoteAddress
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
	IReadOnlyCollection<IPAddress>? ReportedAddresses,
	IPAddress? ReporterRemoteAddress,
	string? Message
);