namespace DeviceStatusBeacon.Services;

/// <summary>
/// 供 Web 管理页面与 CLI 管理命令共享的查询服务。
/// </summary>
/// <remarks>
/// 该服务把“查询范围判断”和“设备 / 日志读取”收敛到同一个地方：
/// Web 可以基于当前登录主体构建受限查询；
/// CLI 则可以显式创建特权查询会话，从而复用相同的投影、筛选和排序规则。
/// </remarks>
public sealed partial class ManagementQueryService(DeviceStatusBeaconContext dbContext) : IManagementQueryService {
	/// <summary>
	/// 首页摘要中展示的设备数量。
	/// </summary>
	private const int DashboardDeviceCount = 4;

	/// <summary>
	/// 首页摘要中展示的日志数量。
	/// </summary>
	private const int DashboardLogCount = 6;

	/// <summary>
	/// 设备查询的默认分页大小。
	/// </summary>
	private const int DevicePageCount = 48;

	/// <summary>
	/// 设备查询允许的最大分页大小。
	/// </summary>
	private const int MaxDeviceQueryCount = 100;

	/// <summary>
	/// 日志查询允许的最大分页大小。
	/// </summary>
	private const int MaxLogQueryCount = 100;
}