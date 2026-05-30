using System.Net;
using System.Security.Claims;

namespace DeviceStatusBeacon.Services;

public sealed partial class ManagementQueryService {
	/// <inheritdoc/>
	public async Task<LogListData> GetLogsAsync(ClaimsPrincipal principal, string? deviceKeyword, int take, CancellationToken cancellationToken = default) =>
		await GetLogsAsync(CreateQuerySessionAsync(principal), deviceKeyword, take, cancellationToken);

	/// <inheritdoc/>
	public async Task<LogListData> GetLogsAsync(ManagementQuerySession session, string? deviceKeyword, int take, CancellationToken cancellationToken = default) {
		// 标准化查询数量和设备关键字
		var normalizedTake = Math.Clamp(take, 10, MaxLogQueryCount);
		var normalizedDeviceKeyword = NormalizeSearchTerm(deviceKeyword);

		// 构建当前可读取的日志范围，并应用设备关键字筛选
		var filteredLogs = ApplyLogDeviceKeyword(BuildAccessibleLogQuery(session), normalizedDeviceKeyword);

		// 统计查询范围内的日志总量，并查询指定数量的日志列表
		var totalCount = await filteredLogs.CountAsync(cancellationToken);
		var logs = await QueryLogsCoreAsync(filteredLogs, normalizedTake, cancellationToken);

		return new(session.ToData(), totalCount, logs);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyCollection<OnlineLogSummary>> QueryLogsAsync(ManagementQuerySession session, LogQueryOptions options, CancellationToken cancellationToken = default) =>
		await QueryLogsCoreAsync(BuildAccessibleLogQuery(session), options, cancellationToken);

	/// <inheritdoc/>
	public Task<IReadOnlyCollection<OnlineLogSummary>> GetDeviceLogsByNameAsync(ManagementQuerySession session, string deviceName, int take, CancellationToken cancellationToken = default) {
		// 标准化查询数量
		var normalizedTake = NormalizeTake(take, 1, MaxLogQueryCount);

		// 构建当前可读取的日志范围，并应用设备名称筛选
		var filteredLogs = ApplyLogDeviceName(BuildAccessibleLogQuery(session), deviceName);

		return QueryLogsCoreAsync(filteredLogs, normalizedTake, cancellationToken);
	}

	/// <summary>
	/// 执行日志列表查询的核心实现。
	/// </summary>
	/// <param name="logs">已应用访问范围过滤的日志查询</param>
	/// <param name="options">日志查询选项</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为日志列表</returns>
	private static Task<IReadOnlyCollection<OnlineLogSummary>> QueryLogsCoreAsync(
		IQueryable<OnlineLog> logs,
		LogQueryOptions options,
		CancellationToken cancellationToken) {
		// 标准化查询数量和设备关键字
		var normalizedDeviceKeyword = NormalizeSearchTerm(options.DeviceKeyword);

		// 将设备关键字筛选应用到日志查询
		logs = ApplyLogDeviceKeyword(logs, normalizedDeviceKeyword);

		// 执行日志列表查询的核心实现
		return QueryLogsCoreAsync(logs, options.Take, cancellationToken);
	}

	/// <summary>
	/// 执行日志列表查询的核心实现。
	/// </summary>
	/// <param name="logs">已应用全部过滤的日志查询</param>
	/// <param name="take">查询数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为日志列表</returns>
	private static async Task<IReadOnlyCollection<OnlineLogSummary>> QueryLogsCoreAsync(
		IQueryable<OnlineLog> logs,
		int take,
		CancellationToken cancellationToken) {
		// 标准化查询数量
		var normalizedTake = NormalizeTake(take, 1, MaxLogQueryCount);

		// 按照日志 ID 降序排序并投影为日志列表项
		var logRows = await logs
			.OrderByDescending(log => log.OnlineLogId)
			.Take(normalizedTake)
			.Select(log => new LogProjection {
				OnlineLogId = log.OnlineLogId,
				DeviceId = log.DeviceId,
				DeviceName = log.Device.DeviceName,
				DeviceDisplayName = log.Device.DisplayName,
				LogTime = log.LogTime,
				ReportedAddresses = log.ReportedAddresses,
				ReporterRemoteAddress = log.ReporterRemoteAddress,
				Message = log.Message
			})
			.ToListAsync(cancellationToken);

		return [.. logRows.Select(MapLogListItem)];
	}

	/// <summary>
	/// 将设备关键字筛选应用到日志查询。
	/// </summary>
	/// <remarks>
	/// 日志搜索使用设备名 / 显示名双字段匹配。
	/// </remarks>
	/// <param name="logs">日志查询</param>
	/// <param name="deviceKeyword">已经规范化的设备筛选关键字</param>
	/// <returns>应用筛选后的日志查询</returns>
	private static IQueryable<OnlineLog> ApplyLogDeviceKeyword(IQueryable<OnlineLog> logs, string? deviceKeyword) =>
		string.IsNullOrWhiteSpace(deviceKeyword)
			? logs
			: logs.Where(log =>
				log.Device.DeviceName.Contains(deviceKeyword)
				|| (log.Device.DisplayName != null && log.Device.DisplayName.Contains(deviceKeyword)));

	/// <summary>
	/// 基于设备名称筛选日志查询。
	/// </summary>
	/// <param name="logs">日志查询</param>
	/// <param name="deviceName">设备名称</param>
	/// <returns>应用筛选后的日志查询</returns>
	private static IQueryable<OnlineLog> ApplyLogDeviceName(IQueryable<OnlineLog> logs, string deviceName) =>
		logs.Where(log => log.Device.DeviceName == deviceName);

	/// <summary>
	/// 基于查询会话构建日志可读取范围查询。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <returns>日志可读取范围查询</returns>
	private IQueryable<OnlineLog> BuildAccessibleLogQuery(ManagementQuerySession session) {
		var logs = dbContext.OnlineLogs.AsNoTracking();

		// 日志访问范围必须和设备访问范围保持一致，避免通过日志侧绕过设备授权。
		return !session.Role.CanQueryAnyDevices()
			? logs.Where(_ => false)
			: session.Role.CanQueryAllDevices()
			? logs
			: session.UserId is Guid userId
			? logs.Where(log => log.Device.AuthorizedUsers.Any(user => user.Id == userId))
			: logs.Where(_ => false);
	}

	/// <summary>
	/// 将日志查询投影映射为日志列表项。
	/// </summary>
	/// <param name="log">日志查询投影</param>
	/// <returns>日志列表项</returns>
	private static OnlineLogSummary MapLogListItem(LogProjection log) => new(
		log.OnlineLogId,
		log.DeviceId,
		log.DeviceName,
		log.DeviceDisplayName,
		log.LogTime,
		[.. log.ReportedAddresses.Select(address => address.ToString())],
		log.ReporterRemoteAddress?.ToString(),
		log.Message);

	/// <summary>
	/// 日志查询投影。
	/// </summary>
	private sealed class LogProjection {
		/// <summary>
		/// 日志 ID
		/// </summary>
		public long OnlineLogId { get; init; }

		/// <summary>
		/// 设备 ID
		/// </summary>
		public Guid DeviceId { get; init; }

		/// <summary>
		/// 设备名称
		/// </summary>
		public string DeviceName { get; init; } = string.Empty;

		/// <summary>
		/// 设备显示名称
		/// </summary>
		public string? DeviceDisplayName { get; init; }

		/// <summary>
		/// 日志时间
		/// </summary>
		public DateTime LogTime { get; init; }

		/// <summary>
		/// 上报地址
		/// </summary>
		public List<IPAddress> ReportedAddresses { get; init; } = [];

		/// <summary>
		/// 上报来源地址
		/// </summary>
		public IPAddress? ReporterRemoteAddress { get; init; }

		/// <summary>
		/// 附加消息
		/// </summary>
		public string? Message { get; init; }
	}
}