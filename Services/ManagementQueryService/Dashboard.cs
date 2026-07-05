using System.Security.Claims;

namespace DeviceStatusBeacon.Services;

public sealed partial class ManagementQueryService {
	/// <inheritdoc/>
	public async Task<DashboardOverviewData> GetDashboardOverviewAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default) =>
		await GetDashboardOverviewAsync(CreateQuerySessionAsync(principal), cancellationToken);

	/// <inheritdoc/>
	public async Task<DashboardOverviewData> GetDashboardOverviewAsync(ManagementQuerySession session, CancellationToken cancellationToken = default) {
		var recentActiveCutoff = DateTime.UtcNow.AddHours(-DashboardRecentActiveWindowHours);

		// 仪表板首屏只同步加载设备侧的轻量统计，避免把日志总量统计放进主渲染路径
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
			deviceMetrics?.RecentActiveDeviceCount ?? 0,
			DashboardRecentActiveWindowHours);
	}

	/// <inheritdoc/>
	public async Task<DashboardActivityData> GetDashboardActivityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default) =>
		await GetDashboardActivityAsync(CreateQuerySessionAsync(principal), cancellationToken);

	/// <inheritdoc/>
	public async Task<DashboardActivityData> GetDashboardActivityAsync(ManagementQuerySession session, CancellationToken cancellationToken = default) {
		// 获取当前查询会话范围内可读取的设备和日志范围，用于列表数据查询
		var accessibleDevices = BuildAccessibleDeviceQuery(session);
		var accessibleLogs = BuildAccessibleLogQuery(session);
		var recentActiveCutoff = DateTime.UtcNow.AddHours(-DashboardRecentActiveWindowHours);

		// 日志总量与最近活动统一延后到同源 API，避免抬高首屏页面渲染成本
		var accessibleLogCount = await accessibleLogs.CountAsync(cancellationToken);

		// 最近活动按设备聚合展示，避免在仪表板中混入过多原始日志明细
		var recentDeviceActivities = await accessibleDevices
			.Where(device => device.LatestLogTime != null && device.LatestLogTime >= recentActiveCutoff)
			.OrderByDescending(device => device.LatestLogTime)
			.ThenBy(device => device.NormalizedDeviceName)
			.Take(DashboardDeviceActivityCount)
			.Select(device => new DeviceActivitySummary(
				device.DeviceName,
				device.DisplayName,
				device.Enabled,
				device.LatestLogTime!.Value, // 上方已经通过 Where 过滤掉了 null 值
				device.LatestReportedAddresses,
				device.LatestReporterRemoteAddress,
				device.OnlineLogs.Count(log => log.LogTime >= recentActiveCutoff)))
			.ToListAsync(cancellationToken);

		return new(accessibleLogCount, recentDeviceActivities);
	}
}