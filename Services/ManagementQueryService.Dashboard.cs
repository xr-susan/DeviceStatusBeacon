using System.Security.Claims;

namespace DeviceStatusBeacon.Services;

public sealed partial class ManagementQueryService {
	/// <inheritdoc/>
	public async Task<DashboardOverviewData> GetDashboardOverviewAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default) =>
		await GetDashboardOverviewAsync(CreateQuerySessionAsync(principal), cancellationToken);

	/// <inheritdoc/>
	public async Task<DashboardOverviewData> GetDashboardOverviewAsync(ManagementQuerySession session, CancellationToken cancellationToken = default) {
		// 获取当前查询会话范围内可读取的设备和日志范围，用于后续统计数据查询
		var accessibleDevices = BuildAccessibleDeviceQuery(session);
		var accessibleLogs = BuildAccessibleLogQuery(session);

		// 统计当前查询会话范围内的设备和日志总量，作为首页摘要的核心指标
		var accessibleDeviceCount = await accessibleDevices.CountAsync(cancellationToken);
		var enabledDeviceCount = await accessibleDevices.CountAsync(device => device.Enabled, cancellationToken);
		var accessibleLogCount = await accessibleLogs.CountAsync(cancellationToken);

		return new(
			session.ToData(),
			accessibleDeviceCount,
			enabledDeviceCount,
			accessibleLogCount);
	}

	/// <inheritdoc/>
	public async Task<DashboardActivityData> GetDashboardActivityAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default) =>
		await GetDashboardActivityAsync(CreateQuerySessionAsync(principal), cancellationToken);

	/// <inheritdoc/>
	public async Task<DashboardActivityData> GetDashboardActivityAsync(ManagementQuerySession session, CancellationToken cancellationToken = default) {
		// 获取当前查询会话范围内可读取的设备和日志范围，用于列表数据查询
		var accessibleDevices = BuildAccessibleDeviceQuery(session);
		var accessibleLogs = BuildAccessibleLogQuery(session);

		// 最近活动不参与首屏关键数据渲染，作为同源 API 的按需加载内容返回
		var recentDevices = await QueryDevicesCoreAsync(
			accessibleDevices,
			DeviceSortMode.RecentActivityDescending,
			DashboardDeviceCount,
			cancellationToken);

		var recentLogs = await QueryLogsCoreAsync(
			accessibleLogs,
			DashboardLogCount,
			cancellationToken);

		return new(recentDevices, recentLogs);
	}
}