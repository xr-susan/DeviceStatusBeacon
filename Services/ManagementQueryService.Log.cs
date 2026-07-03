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
		var filteredLogs = BuildAccessibleLogQuery(session)
			.WhereDeviceNormalizedDeviceName(normalizedDeviceName);

		return QueryLogsPageAsync(filteredLogs, 0, normalizedTake, cancellationToken);
	}

	/// <inheritdoc/>
	public Task<IReadOnlyCollection<OnlineLogSummary>> GetLogsByDeviceIdAsync(ManagementQuerySession session, Guid deviceId, int take, CancellationToken cancellationToken = default) {
		// 标准化查询数量
		var normalizedTake = NormalizePageSize(take, 1, MaxLogQueryCount);

		// 构建当前可读取的日志范围，并应用设备 ID 筛选
		var filteredLogs = BuildAccessibleLogQuery(session)
			.WhereDeviceId(deviceId);

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
				: logs.Where(log => log.Device.DisplayName != null
					&& log.Device.DisplayName.Contains(normalizedDisplayNameKeyword));
		}

		// 设备名称关键字不为空，则按设备名称关键字和显示名称关键字任一匹配筛选
		return string.IsNullOrWhiteSpace(normalizedDisplayNameKeyword)
			? logs.Where(log => log.Device.NormalizedDeviceName.Contains(normalizedDeviceNameKeyword))
			: logs.Where(log =>
				log.Device.NormalizedDeviceName.Contains(normalizedDeviceNameKeyword)
				|| (log.Device.DisplayName != null && log.Device.DisplayName.Contains(normalizedDisplayNameKeyword)));
	}

	/// <summary>
	/// 基于查询会话构建日志可读取范围查询。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <returns>日志可读取范围查询</returns>
	private IQueryable<OnlineLog> BuildAccessibleLogQuery(ManagementQuerySession session) {
		var logs = dbContext.OnlineLogs.AsNoTracking();

		return session.Role.GetDeviceQueryScope() switch {
			// 全量查询权限，返回完整查询
			PrincipalQueryScope.Full => logs,

			// 具备有限查询权限时，根据授权主体类型应用对应的设备授权关系
			PrincipalQueryScope.Limited => session.PrincipalKind switch {
				// 用户主体，返回已授权给该用户的设备日志
				ManagementQueryPrincipalKind.User =>
					logs.Where(log => log.Device.AuthorizedUsers.Any(user => user.Id == session.PrincipalId)),

				// API 凭据主体，返回已授权给该 API 凭据的设备日志
				ManagementQueryPrincipalKind.ApiCredential =>
					logs.Where(log => log.Device.AuthorizedApiCredentials.Any(credential => credential.ApiCredentialId == session.PrincipalId)),

				// 其他主体类型不具备有限查询权限，返回空查询
				_ => logs.Where(_ => false)
			},

			// 无查询权限，返回空查询
			_ => logs.Where(_ => false)
		};
	}
}