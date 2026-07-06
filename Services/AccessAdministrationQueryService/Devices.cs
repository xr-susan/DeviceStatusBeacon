namespace DeviceStatusBeacon.Services;

public sealed partial class AccessAdministrationQueryService {
	/// <inheritdoc/>
	public async Task<DeviceAuthorizedUsersData?> GetDeviceAuthorizedUsersAsync(Guid deviceId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
		await GetDeviceAuthorizedUsersAsync(
			dbContext.Devices.AsNoTracking().WhereDeviceId(deviceId),
			pageNumber,
			pageSize,
			cancellationToken);

	/// <inheritdoc/>
	public async Task<DeviceAuthorizedUsersData?> GetDeviceAuthorizedUsersAsync(string deviceName, int pageNumber, int pageSize, CancellationToken cancellationToken = default) {
		var deviceNameLookup = IdentityNameLookup.TryCreate(deviceName, lookupNormalizer);
		return deviceNameLookup is null
			? null
			: await GetDeviceAuthorizedUsersAsync(
				dbContext.Devices.AsNoTracking().WhereDeviceName(deviceNameLookup),
				pageNumber,
				pageSize,
				cancellationToken);
	}

	/// <summary>
	/// 获取访问管理设备授权用户数据。
	/// </summary>
	/// <param name="devices">已经限定目标设备范围的设备查询</param>
	/// <param name="pageNumber">页码</param>
	/// <param name="pageSize">每页数量</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为访问管理设备授权用户数据；如果未找到设备，则返回 null</returns>
	private async Task<DeviceAuthorizedUsersData?> GetDeviceAuthorizedUsersAsync(IQueryable<Device> devices, int pageNumber, int pageSize, CancellationToken cancellationToken) {
		var device = await devices
			.Select(entity => new {
				entity.DeviceId,
				entity.DeviceName,
				entity.DisplayName,
				AuthorizedUserCount = entity.AuthorizedUserLinks.Count
			})
			.SingleOrDefaultAsync(cancellationToken);
		if (device is null) {
			return null;
		}

		var normalizedPageNumber = NormalizePageNumber(pageNumber);
		var normalizedPageSize = NormalizePageSize(pageSize);

		normalizedPageNumber = NormalizePageNumberForTotalCount(normalizedPageNumber, normalizedPageSize, device.AuthorizedUserCount);

		var authorizedUsersQuery = dbContext.DeviceUsers
			.AsNoTracking()
			.Where(authorization => authorization.AuthorizedDevicesDeviceId == device.DeviceId)
			.Select(authorization => authorization.AuthorizedUser);

		var authorizedUsers = await QueryUsersPageAsync(
			authorizedUsersQuery,
			CalculateSkipCount(normalizedPageNumber, normalizedPageSize),
			normalizedPageSize,
			cancellationToken);

		return new(
			device.DeviceId,
			device.DeviceName,
			device.DisplayName,
			new(device.AuthorizedUserCount, normalizedPageNumber, normalizedPageSize),
			authorizedUsers);
	}
}