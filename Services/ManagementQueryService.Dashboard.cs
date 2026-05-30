using System.Security.Claims;

namespace DeviceStatusBeacon.Services;

public sealed partial class ManagementQueryService {
	/// <inheritdoc/>
	public async Task<DashboardData> GetDashboardAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default) =>
		await GetDashboardAsync(CreateQuerySessionAsync(principal), cancellationToken);

	/// <inheritdoc/>
	public async Task<DashboardData> GetDashboardAsync(ManagementQuerySession session, CancellationToken cancellationToken = default) {
		// 获取当前查询会话范围内可读取的设备和日志范围，用于后续统计和列表查询
		var accessibleDevices = BuildAccessibleDeviceQuery(session);
		var accessibleLogs = BuildAccessibleLogQuery(session);

		// 统计当前查询会话范围内的设备和日志总量，作为首页摘要的核心指标
		var accessibleDeviceCount = await accessibleDevices.CountAsync(cancellationToken);
		var enabledDeviceCount = await accessibleDevices.CountAsync(device => device.Enabled, cancellationToken);
		var accessibleLogCount = await accessibleLogs.CountAsync(cancellationToken);

		// 查询最近活跃的设备和日志，作为首页摘要的亮点内容
		var highlightedDevices = await QueryDevicesCoreAsync(
			accessibleDevices,
			DeviceSortMode.RecentActivityDescending,
			DashboardDeviceCount,
			cancellationToken);

		var recentLogs = await QueryLogsCoreAsync(
			accessibleLogs,
			DashboardLogCount,
			cancellationToken);

		return new(
			session.ToData(),
			accessibleDeviceCount,
			enabledDeviceCount,
			accessibleLogCount,
			highlightedDevices,
			recentLogs);
	}
}