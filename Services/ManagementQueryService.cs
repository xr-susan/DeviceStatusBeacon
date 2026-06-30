using Microsoft.AspNetCore.Identity;

namespace DeviceStatusBeacon.Services;

/// <summary>
/// 供 Web 管理页面与 CLI 管理命令共享的查询服务。
/// </summary>
/// <remarks>
/// 该服务把“查询范围判断”和“设备 / 日志读取”收敛到同一个地方：
/// Web 可以基于当前登录主体构建受限查询；
/// CLI 则可以显式创建特权查询会话，从而复用相同的投影、筛选和排序规则。
/// </remarks>
public sealed partial class ManagementQueryService(DeviceStatusBeaconContext dbContext, ILookupNormalizer lookupNormalizer) : IManagementQueryService {
	/// <summary>
	/// 首页摘要中展示的设备数量。
	/// </summary>
	private const int DashboardDeviceActivityCount = 8;

	/// <summary>
	/// Dashboard 中“近期活跃设备”统计使用的时间窗口（小时）。
	/// </summary>
	private const int DashboardRecentActiveWindowHours = 24;

	/// <summary>
	/// 设备查询允许的最大分页大小。
	/// </summary>
	private const int MaxDeviceQueryCount = 50;

	/// <summary>
	/// 日志查询允许的最大分页大小。
	/// </summary>
	private const int MaxLogQueryCount = 100;
}