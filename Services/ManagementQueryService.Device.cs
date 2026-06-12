using System.Net;
using System.Security.Claims;

namespace DeviceStatusBeacon.Services;

public sealed partial class ManagementQueryService {
	/// <inheritdoc/>
	public async Task<DeviceListData> GetDevicesAsync(ClaimsPrincipal principal, string? searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
		await GetDevicesAsync(CreateQuerySessionAsync(principal), searchTerm, pageNumber, pageSize, cancellationToken);

	/// <inheritdoc/>
	public async Task<DeviceListData> GetDevicesAsync(ManagementQuerySession session, string? searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default) {
		// 标准化分页选项和查询关键字
		var normalizedPageNumber = NormalizePageNumber(pageNumber);
		var normalizedPageSize = NormalizePageSize(pageSize, 1, MaxDeviceQueryCount);
		var normalizedDeviceNameSearchTerm = NormalizeDeviceName(searchTerm);
		var normalizedDisplayNameSearchTerm = NormalizeDisplayNameSearchTerm(searchTerm);

		// 构建当前可读取的设备范围，并应用关键字筛选
		var filteredDevices = ApplyDeviceSearchTerm(BuildAccessibleDeviceQuery(session), normalizedDeviceNameSearchTerm, normalizedDisplayNameSearchTerm);

		// 统计查询范围内的设备总量，并按实际总页数纠正页码
		var totalCount = await filteredDevices.CountAsync(cancellationToken);
		normalizedPageNumber = NormalizePageNumberForTotalCount(normalizedPageNumber, normalizedPageSize, totalCount);

		// 查询当前页的设备列表
		var devices = await QueryDevicesPageAsync(
			filteredDevices,
			CalculateSkipCount(normalizedPageNumber, normalizedPageSize),
			normalizedPageSize,
			sortByDeviceName: false,
			cancellationToken);

		return new(
			session.ToData(),
			new(totalCount, normalizedPageNumber, normalizedPageSize),
			devices);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyCollection<DeviceSummary>> GetDeviceSliceAsync(ManagementQuerySession session, string? searchTerm, int take, bool sortByDeviceName = false, CancellationToken cancellationToken = default) {
		// 标准化查询数量和查询关键字
		var normalizedTake = NormalizePageSize(take, 1, MaxDeviceQueryCount);
		var normalizedDeviceNameSearchTerm = NormalizeDeviceName(searchTerm);
		var normalizedDisplayNameSearchTerm = NormalizeDisplayNameSearchTerm(searchTerm);

		// 构建当前可读取的设备范围，并应用关键字筛选
		var filteredDevices = ApplyDeviceSearchTerm(BuildAccessibleDeviceQuery(session), normalizedDeviceNameSearchTerm, normalizedDisplayNameSearchTerm);

		return await QueryDevicesPageAsync(
			filteredDevices,
			0,
			normalizedTake,
			sortByDeviceName,
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<DeviceSummary?> GetDeviceByNameAsync(ManagementQuerySession session, string deviceName, CancellationToken cancellationToken = default) {
		var normalizedDeviceName = NormalizeDeviceName(deviceName);
		if (normalizedDeviceName is null) {
			return null;
		}

		// 构建当前可读取的设备范围，并应用设备名称筛选
		var filteredDevices = ApplyDeviceName(BuildAccessibleDeviceQuery(session), normalizedDeviceName);

		// 显式查询单个设备
		var deviceRow = await ApplyDeviceProjection(filteredDevices)
			.SingleOrDefaultAsync(cancellationToken);

		return deviceRow is null ? null : MapDeviceListItem(deviceRow);
	}

	/// <summary>
	/// 查询设备分页数据。
	/// </summary>
	/// <param name="devices">已应用全部过滤的设备查询</param>
	/// <param name="skip">已经规范化的跳过数量</param>
	/// <param name="take">已经规范化的查询数量</param>
	/// <param name="sortByDeviceName">是否按设备名称升序排序</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为设备列表</returns>
	private static async Task<IReadOnlyCollection<DeviceSummary>> QueryDevicesPageAsync(
		IQueryable<Device> devices,
		int skip,
		int take,
		bool sortByDeviceName,
		CancellationToken cancellationToken) {
		// 按指定方式排序、投影并执行查询
		var projectedDevices = ApplyDeviceProjection(devices);
		projectedDevices = sortByDeviceName
			? projectedDevices.OrderBy(device => device.DeviceName)
			: projectedDevices
				.OrderByDescending(device => device.LatestLogTime)
				.ThenBy(device => device.DeviceName);

		var deviceRows = await projectedDevices
			.Skip(skip)
			.Take(take)
			.ToListAsync(cancellationToken);

		return [.. deviceRows.Select(MapDeviceListItem)];
	}

	/// <summary>
	/// 将设备关键字筛选应用到设备查询。
	/// </summary>
	/// <remarks>
	/// 设备搜索使用归一化设备名 / 显示名双字段匹配。
	/// </remarks>
	/// <param name="devices">设备查询</param>
	/// <param name="normalizedDeviceNameSearchTerm">已经归一化的设备名称筛选关键字</param>
	/// <param name="normalizedDisplayNameSearchTerm">去掉首尾空白后的显示名称筛选关键字</param>
	/// <returns>应用筛选后的设备查询</returns>
	private static IQueryable<Device> ApplyDeviceSearchTerm(
		IQueryable<Device> devices,
		string? normalizedDeviceNameSearchTerm,
		string? normalizedDisplayNameSearchTerm) {
		// 设备名称关键字为空，则按显示名称关键字（如果有）筛选
		if (string.IsNullOrWhiteSpace(normalizedDeviceNameSearchTerm)) {
			return string.IsNullOrWhiteSpace(normalizedDisplayNameSearchTerm)
				? devices
				: devices.Where(device => device.DisplayName != null
					&& device.DisplayName.Contains(normalizedDisplayNameSearchTerm)); // skipcq: CS-R1136 表达式树不支持 is 模式匹配
		}

		// 设备名称关键字不为空，则按设备名称关键字和显示名称关键字任一匹配筛选
		return string.IsNullOrWhiteSpace(normalizedDisplayNameSearchTerm)
			? devices.Where(device => device.NormalizedDeviceName.Contains(normalizedDeviceNameSearchTerm))
			: devices.Where(device =>
				device.NormalizedDeviceName.Contains(normalizedDeviceNameSearchTerm)
				|| (device.DisplayName != null && device.DisplayName.Contains(normalizedDisplayNameSearchTerm))); // skipcq: CS-R1136 表达式树不支持 is 模式匹配
	}

	/// <summary>
	/// 基于设备名称筛选设备查询。
	/// </summary>
	/// <param name="devices">设备查询</param>
	/// <param name="normalizedDeviceName">已经归一化的设备名称</param>
	/// <returns>应用筛选后的设备查询</returns>
	private static IQueryable<Device> ApplyDeviceName(IQueryable<Device> devices, string normalizedDeviceName) =>
		devices.Where(device => device.NormalizedDeviceName == normalizedDeviceName);

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

		// 无查询权限，返回空查询
		if (!session.Role.CanQueryAnyDevices()) {
			return devices.Where(_ => false);
		}

		// 全量查询权限，返回完整查询
		if (session.Role.CanQueryAllDevices()) {
			return devices;
		}

		// 具备有限查询权限，返回关联了该用户的设备的查询
		if (session.UserId is Guid userId) {
			return devices.Where(device => device.AuthorizedUsers.Any(user => user.Id == userId));
		}

		// 其他情况，返回空查询
		return devices.Where(_ => false);
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