using System.Security.Claims;

namespace DeviceStatusBeacon.Services;

public sealed partial class ManagementQueryService {
	/// <inheritdoc/>
	public async Task<DashboardOverviewData> GetDashboardOverviewAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default) =>
		await GetDashboardOverviewAsync(CreateQuerySessionAsync(principal), cancellationToken);

	/// <inheritdoc/>
	public async Task<DashboardOverviewData> GetDashboardOverviewAsync(ManagementQuerySession session, CancellationToken cancellationToken = default) {
		var recentActiveCutoff = DateTime.UtcNow.AddHours(-DashboardRecentActiveWindowHours);

		// 首页首屏只同步加载设备侧的轻量统计，避免把日志总量统计放进主渲染路径
		var deviceMetrics = await BuildAccessibleDeviceQuery(session)
			.GroupBy(_ => 1)
			.Select(group => new {
				AccessibleDeviceCount = group.Count(),
				EnabledDeviceCount = group.Count(device => device.Enabled),
				RecentActiveDeviceCount = group.Count(device =>
					device.LatestLogTime != null
					&& device.LatestLogTime >= recentActiveCutoff)
			})
			.SingleOrDefaultAsync(cancellationToken);

		return new(
			session.ToData(),
			deviceMetrics?.AccessibleDeviceCount ?? 0,
			deviceMetrics?.EnabledDeviceCount ?? 0,
			deviceMetrics?.RecentActiveDeviceCount ?? 0);
	}

	/// <inheritdoc/>
	public async Task<DashboardActivityData> GetDashboardActivityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default) =>
		await GetDashboardActivityAsync(CreateQuerySessionAsync(principal), cancellationToken);

	/// <inheritdoc/>
	public async Task<DashboardActivityData> GetDashboardActivityAsync(ManagementQuerySession session, CancellationToken cancellationToken = default) {
		// 获取当前查询会话范围内可读取的设备和日志范围，用于列表数据查询
		var accessibleDevices = BuildAccessibleDeviceQuery(session);
		var accessibleLogs = BuildAccessibleLogQuery(session);

		// 日志总量与最近活动统一延后到同源 API，避免抬高首屏页面渲染成本
		var accessibleLogCount = await accessibleLogs.CountAsync(cancellationToken);

		// 最近活动不参与首屏关键数据渲染，作为同源 API 的按需加载内容返回
		var recentDevices = await QueryDevicesPageAsync(
			accessibleDevices,
			0,
			DashboardDeviceCount,
			sortByNormalizedDeviceName: false,
			cancellationToken);

		var recentLogs = await QueryLogsPageAsync(
			accessibleLogs,
			0,
			DashboardLogCount,
			cancellationToken);

		return new(accessibleLogCount, recentDevices, recentLogs);
	}
}