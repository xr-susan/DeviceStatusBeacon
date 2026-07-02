using System.Security.Claims;

namespace DeviceStatusBeacon.Services;

public sealed partial class ManagementQueryService {
	/// <inheritdoc/>
	public async Task<LogListData> GetLogsAsync(ClaimsPrincipal principal, string? deviceKeyword, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
		await GetLogsAsync(CreateQuerySessionAsync(principal), deviceKeyword, pageNumber, pageSize, cancellationToken);

	/// <inheritdoc/>
	public async Task<LogListData> GetLogsAsync(ManagementQuerySession session, string? deviceKeyword, int pageNumber, int pageSize, CancellationToken cancellationToken = default) {
		// 标准化分页选项和设备关键字
		var normalizedPageNumber = NormalizePageNumber(pageNumber);
		var normalizedPageSize = NormalizePageSize(pageSize, 1, MaxLogQueryCount);
		var normalizedDeviceNameKeyword = NormalizeDeviceName(deviceKeyword);
		var normalizedDisplayNameKeyword = NormalizeDisplayNameSearchTerm(deviceKeyword);

		// 构建当前可读取的日志范围，并应用设备关键字筛选
		var filteredLogs = ApplyLogDeviceKeyword(BuildAccessibleLogQuery(session), normalizedDeviceNameKeyword, normalizedDisplayNameKeyword);

		// 统计查询范围内的日志总量，并按实际总页数纠正页码
		var totalCount = await filteredLogs.CountAsync(cancellationToken);
		normalizedPageNumber = NormalizePageNumberForTotalCount(normalizedPageNumber, normalizedPageSize, totalCount);

		// 查询当前页的日志列表
		var logs = await QueryLogsPageAsync(
			filteredLogs,
			CalculateSkipCount(normalizedPageNumber, normalizedPageSize),
			normalizedPageSize,
			cancellationToken);

		return new(
			session.ToData(),
			new(totalCount, normalizedPageNumber, normalizedPageSize),
			logs);
	}

	/// <inheritdoc/>
	public Task<IReadOnlyCollection<OnlineLogSummary>> GetLogsByDeviceNameAsync(ManagementQuerySession session, string deviceName, int take, CancellationToken cancellationToken = default) {
		var normalizedDeviceName = NormalizeDeviceName(deviceName);
		if (normalizedDeviceName is null) {
			return Task.FromResult<IReadOnlyCollection<OnlineLogSummary>>([]);
		}

		// 标准化查询数量
		var normalizedTake = NormalizePageSize(take, 1, MaxLogQueryCount);

		// 构建当前可读取的日志范围，并应用设备名称筛选
		var filteredLogs = ApplyLogDeviceName(BuildAccessibleLogQuery(session), normalizedDeviceName);

		return QueryLogsPageAsync(filteredLogs, 0, normalizedTake, cancellationToken);
	}

	/// <inheritdoc/>
	public Task<IReadOnlyCollection<OnlineLogSummary>> GetLogsByDeviceIdAsync(ManagementQuerySession session, Guid deviceId, int take, CancellationToken cancellationToken = default) {
		// 标准化查询数量
		var normalizedTake = NormalizePageSize(take, 1, MaxLogQueryCount);

		// 构建当前可读取的日志范围，并应用设备 ID 筛选
		var filteredLogs = ApplyLogDeviceId(BuildAccessibleLogQuery(session), deviceId);

		return QueryLogsPageAsync(filteredLogs, 0, normalizedTake, cancellationToken);
	}

	/// <summary>
	/// 查询日志分页数据。
	/// </summary>
	/// <param name="logs">已应用全部过滤的日志查询</param>
	/// <param name="skip">已经规范化的跳过数量</param>
	/// <param name="take">已经规范化的查询数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为日志列表</returns>
	private static async Task<IReadOnlyCollection<OnlineLogSummary>> QueryLogsPageAsync(
		IQueryable<OnlineLog> logs,
		int skip,
		int take,
		CancellationToken cancellationToken) =>
		// 投影为 OnlineLogSummary 并按照日志 ID 降序排序
		await logs
			.OrderByDescending(log => log.OnlineLogId)
			.Skip(skip)
			.Take(take)
			.Select(log => new OnlineLogSummary(
				log.OnlineLogId,
				log.DeviceId,
				log.Device.DeviceName,
				log.Device.DisplayName,
				log.LogTime,
				log.ReportedAddresses,
				log.ReporterRemoteAddress,
				log.Message))
			.ToListAsync(cancellationToken);

	/// <summary>
	/// 将设备关键字筛选应用到日志查询。
	/// </summary>
	/// <remarks>
	/// 日志搜索使用归一化设备名 / 显示名双字段匹配。
	/// </remarks>
	/// <param name="logs">日志查询</param>
	/// <param name="normalizedDeviceNameKeyword">已经归一化的设备名称筛选关键字</param>
	/// <param name="normalizedDisplayNameKeyword">去掉首尾空白后的显示名称筛选关键字</param>
	/// <returns>应用筛选后的日志查询</returns>
	private static IQueryable<OnlineLog> ApplyLogDeviceKeyword(
		IQueryable<OnlineLog> logs,
		string? normalizedDeviceNameKeyword,
		string? normalizedDisplayNameKeyword) {
		// 设备名称关键字为空，则按显示名称关键字（如果有）筛选
		if (string.IsNullOrWhiteSpace(normalizedDeviceNameKeyword)) {
			return string.IsNullOrWhiteSpace(normalizedDisplayNameKeyword)
				? logs
				: logs.Where(log => log.Device.DisplayName != null // skipcq: CS-R1136 表达式树不支持 is 模式匹配
					&& log.Device.DisplayName.Contains(normalizedDisplayNameKeyword));
		}

		// 设备名称关键字不为空，则按设备名称关键字和显示名称关键字任一匹配筛选
		return string.IsNullOrWhiteSpace(normalizedDisplayNameKeyword)
			? logs.Where(log => log.Device.NormalizedDeviceName.Contains(normalizedDeviceNameKeyword))
			: logs.Where(log =>
				log.Device.NormalizedDeviceName.Contains(normalizedDeviceNameKeyword)
				|| (log.Device.DisplayName != null && log.Device.DisplayName.Contains(normalizedDisplayNameKeyword))); // skipcq: CS-R1136 表达式树不支持 is 模式匹配
	}

	/// <summary>
	/// 基于设备名称筛选日志查询。
	/// </summary>
	/// <param name="logs">日志查询</param>
	/// <param name="normalizedDeviceName">已经归一化的设备名称</param>
	/// <returns>应用筛选后的日志查询</returns>
	private static IQueryable<OnlineLog> ApplyLogDeviceName(IQueryable<OnlineLog> logs, string normalizedDeviceName) =>
		logs.Where(log => log.Device.NormalizedDeviceName == normalizedDeviceName);

	/// <summary>
	/// 基于设备 ID 筛选日志查询。
	/// </summary>
	/// <param name="logs">日志查询</param>
	/// <param name="deviceId">设备 ID</param>
	/// <returns>应用筛选后的日志查询</returns>
	private static IQueryable<OnlineLog> ApplyLogDeviceId(IQueryable<OnlineLog> logs, Guid deviceId) =>
		logs.Where(log => log.DeviceId == deviceId);

	/// <summary>
	/// 基于查询会话构建日志可读取范围查询。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <returns>日志可读取范围查询</returns>
	private IQueryable<OnlineLog> BuildAccessibleLogQuery(ManagementQuerySession session) {
		var logs = dbContext.OnlineLogs.AsNoTracking();

		// 无查询权限，返回空查询
		if (!session.Role.CanQueryAnyDevices()) {
			return logs.Where(_ => false);
		}

		// 全量查询权限，返回完整查询
		if (session.Role.CanQueryAllDevices()) {
			return logs;
		}

		// 具备有限查询权限，返回关联了该用户的设备的日志查询
		if (session.UserId is Guid userId) {
			return logs.Where(log => log.Device.AuthorizedUsers.Any(user => user.Id == userId));
		}

		// 其他情况，返回空查询
		return logs.Where(_ => false);
	}
}