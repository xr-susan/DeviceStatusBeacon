using System.Net;
using System.Security.Claims;

namespace DeviceStatusBeacon.Services;

public sealed partial class ManagementQueryService {
	/// <inheritdoc/>
	public async Task<DeviceListData> GetDevicesAsync(ClaimsPrincipal principal, string? searchTerm, CancellationToken cancellationToken = default) =>
		await GetDevicesAsync(CreateQuerySessionAsync(principal), searchTerm, cancellationToken);

	/// <inheritdoc/>
	public async Task<DeviceListData> GetDevicesAsync(ManagementQuerySession session, string? searchTerm, CancellationToken cancellationToken = default) {
		// 标准化查询关键字
		var normalizedSearchTerm = NormalizeSearchTerm(searchTerm);

		// 构建当前可读取的设备范围，并应用关键字筛选
		var filteredDevices = ApplyDeviceSearchTerm(BuildAccessibleDeviceQuery(session), normalizedSearchTerm);

		// 统计查询范围内的设备总量，并查询默认数量的设备列表
		var totalCount = await filteredDevices.CountAsync(cancellationToken);
		var devices = await QueryDevicesCoreAsync(
			filteredDevices,
			DeviceSortMode.RecentActivityDescending,
			DevicePageCount,
			cancellationToken);

		return new(session.ToData(), totalCount, devices);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyCollection<DeviceSummary>> QueryDevicesAsync(ManagementQuerySession session, DeviceQueryOptions options, CancellationToken cancellationToken = default) =>
		await QueryDevicesCoreAsync(BuildAccessibleDeviceQuery(session), options, cancellationToken);

	/// <inheritdoc/>
	public async Task<DeviceSummary?> GetDeviceByNameAsync(ManagementQuerySession session, string deviceName, CancellationToken cancellationToken = default) {
		// 构建当前可读取的设备范围，并应用设备名称筛选
		var filteredDevices = ApplyDeviceName(BuildAccessibleDeviceQuery(session), deviceName);

		// 显式查询单个设备
		var deviceRow = await ApplyDeviceProjection(filteredDevices)
			.SingleOrDefaultAsync(cancellationToken);

		return deviceRow is null ? null : MapDeviceListItem(deviceRow);
	}

	/// <summary>
	/// 执行设备列表查询的核心实现。
	/// </summary>
	/// <param name="devices">已应用访问范围过滤的设备查询</param>
	/// <param name="options">设备查询选项</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为设备列表</returns>
	private static Task<IReadOnlyCollection<DeviceSummary>> QueryDevicesCoreAsync(
		IQueryable<Device> devices,
		DeviceQueryOptions options,
		CancellationToken cancellationToken) {
		// 标准化查询关键字
		var normalizedSearchTerm = NormalizeSearchTerm(options.SearchTerm);

		// 将设备关键字筛选应用到设备查询
		devices = ApplyDeviceSearchTerm(devices, normalizedSearchTerm);

		// 执行设备列表查询的核心实现
		return QueryDevicesCoreAsync(devices, options.SortMode, options.Take, cancellationToken);
	}

	/// <summary>
	/// 执行设备列表查询的核心实现。
	/// </summary>
	/// <param name="devices">已应用全部过滤的设备查询</param>
	/// <param name="sortMode">排序方式</param>
	/// <param name="take">查询数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为设备列表</returns>
	private static async Task<IReadOnlyCollection<DeviceSummary>> QueryDevicesCoreAsync(
		IQueryable<Device> devices,
		DeviceSortMode sortMode,
		int take,
		CancellationToken cancellationToken) {
		// 标准化查询数量
		var normalizedTake = NormalizeTake(take, 1, MaxDeviceQueryCount);

		// 按照指定排序方式排序并投影为设备列表项
		devices = sortMode switch {
			DeviceSortMode.DeviceNameAscending => devices.OrderBy(device => device.DeviceName),
			_ => devices
				.OrderByDescending(device => device.LatestLogTime)
				.ThenBy(device => device.DeviceName)
		};

		// 投影查询并执行
		var deviceRows = await ApplyDeviceProjection(devices)
			.Take(normalizedTake)
			.ToListAsync(cancellationToken);

		return [.. deviceRows.Select(MapDeviceListItem)];
	}

	/// <summary>
	/// 将设备关键字筛选应用到设备查询。
	/// </summary>
	/// <remarks>
	/// 设备搜索使用设备名 / 显示名双字段匹配。
	/// </remarks>
	/// <param name="devices">设备查询</param>
	/// <param name="searchTerm">已经规范化的筛选关键字</param>
	/// <returns>应用筛选后的设备查询</returns>
	private static IQueryable<Device> ApplyDeviceSearchTerm(IQueryable<Device> devices, string? searchTerm) =>
		string.IsNullOrWhiteSpace(searchTerm)
			? devices
			: devices.Where(device =>
				device.DeviceName.Contains(searchTerm)
				|| (device.DisplayName != null && device.DisplayName.Contains(searchTerm)));

	/// <summary>
	/// 基于设备名称筛选设备查询。
	/// </summary>
	/// <param name="devices">设备查询</param>
	/// <param name="deviceName">设备名称</param>
	/// <returns>应用筛选后的设备查询</returns>
	private static IQueryable<Device> ApplyDeviceName(IQueryable<Device> devices, string deviceName) =>
		devices.Where(device => device.DeviceName == deviceName);

	/// <summary>
	/// 将设备查询投影为列表需要的字段。
	/// </summary>
	/// <param name="devices">设备查询</param>
	/// <returns>设备投影查询</returns>
	private static IQueryable<DeviceProjection> ApplyDeviceProjection(IQueryable<Device> devices) =>
		devices.Select(device => new DeviceProjection {
			DeviceId = device.DeviceId,
			DeviceName = device.DeviceName,
			DisplayName = device.DisplayName,
			Enabled = device.Enabled,
			LatestLogTime = device.LatestLogTime,
			LatestReportedAddresses = device.LatestReportedAddresses,
			LatestReporterRemoteAddress = device.LatestReporterRemoteAddress
		});

	/// <summary>
	/// 基于查询会话构建设备可读取范围查询。
	/// </summary>
	/// <param name="session">查询会话</param>
	/// <returns>设备可读取范围查询</returns>
	private IQueryable<Device> BuildAccessibleDeviceQuery(ManagementQuerySession session) {
		var devices = dbContext.Devices.AsNoTracking();

		// 如果当前主体完全不具备设备数据查询能力，则直接收敛为空查询。
		return !session.Role.CanQueryAnyDevices()
			? devices.Where(_ => false)
			: session.Role.CanQueryAllDevices()
			? devices
			: session.UserId is Guid userId
			? devices.Where(device => device.AuthorizedUsers.Any(user => user.Id == userId))
			: devices.Where(_ => false);
	}

	/// <summary>
	/// 将设备查询投影映射为设备列表项。
	/// </summary>
	/// <param name="device">设备查询投影</param>
	/// <returns>设备列表项</returns>
	private static DeviceSummary MapDeviceListItem(DeviceProjection device) => new(
		device.DeviceId,
		device.DeviceName,
		device.DisplayName,
		device.Enabled,
		device.LatestLogTime,
		device.LatestReportedAddresses is null
			? []
			: [.. device.LatestReportedAddresses.Select(address => address.ToString())],
		device.LatestReporterRemoteAddress?.ToString());

	/// <summary>
	/// 设备查询投影。
	/// </summary>
	private sealed class DeviceProjection {
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
		public string? DisplayName { get; init; }

		/// <summary>
		/// 设备是否启用
		/// </summary>
		public bool Enabled { get; init; }

		/// <summary>
		/// 最近日志时间
		/// </summary>
		public DateTime? LatestLogTime { get; init; }

		/// <summary>
		/// 最近上报地址
		/// </summary>
		public List<IPAddress>? LatestReportedAddresses { get; init; }

		/// <summary>
		/// 最近上报来源地址
		/// </summary>
		public IPAddress? LatestReporterRemoteAddress { get; init; }
	}
}