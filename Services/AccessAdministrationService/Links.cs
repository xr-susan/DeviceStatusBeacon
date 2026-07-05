namespace DeviceStatusBeacon.Services;

public sealed partial class AccessAdministrationService {
	/// <summary>
	/// 更新授权设备列表。
	/// </summary>
	/// <param name="userId">用户 ID</param>
	/// <param name="authorizedDeviceIds">新的授权设备 ID 列表</param>
	/// <param name="isNewUser">是否为新建用户</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>规范化后的授权设备 ID 集合</returns>
	private async Task<HashSet<Guid>> UpdateAuthorizedDeviceLinksAsync(
		Guid userId,
		IReadOnlyCollection<Guid>? authorizedDeviceIds,
		bool isNewUser,
		CancellationToken cancellationToken) {
		// 规范化授权设备 ID 列表，去除 Guid.Empty
		var requestedDeviceIds = authorizedDeviceIds?
			.Where(deviceId => deviceId != Guid.Empty)
			.ToHashSet() ?? [];

		// 取出当前授权设备 ID 列表
		// 新建用户没有任何授权设备，直接使用空集合
		var currentDeviceIds = isNewUser
			? []
			: await dbContext.DeviceUsers
				.AsNoTracking()
				.Where(authorization => authorization.AuthorizedUsersId == userId)
				.Select(authorization => authorization.AuthorizedDevicesDeviceId)
				.ToHashSetAsync(cancellationToken);

		// 计算需要移除和新增的授权设备 ID
		var removedDeviceIds = currentDeviceIds.Except(requestedDeviceIds).ToHashSet();
		var addedDeviceIds = requestedDeviceIds.Except(currentDeviceIds).ToHashSet();

		// 先校验新增目标设备存在，再更新中间表；否则业务异常后会留下被局部修改的跟踪状态
		if (addedDeviceIds.Count > 0) {
			var existingAddedDeviceCount = await dbContext.Devices
				.AsNoTracking()
				.Where(device => addedDeviceIds.Contains(device.DeviceId))
				.CountAsync(cancellationToken);
			if (existingAddedDeviceCount != addedDeviceIds.Count) {
				throw new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, "授权设备范围包含不存在的设备");
			}
		}

		// 先移除被裁剪的授权设备
		if (removedDeviceIds.Count > 0) {
			var removedAuthorizations = await dbContext.DeviceUsers
				.Where(authorization => authorization.AuthorizedUsersId == userId
					&& removedDeviceIds.Contains(authorization.AuthorizedDevicesDeviceId))
				.ToListAsync(cancellationToken);
			dbContext.DeviceUsers.RemoveRange(removedAuthorizations);
		}

		// 再新增新的授权设备
		foreach (var deviceId in addedDeviceIds) {
			dbContext.DeviceUsers.Add(new DeviceUser {
				AuthorizedDevicesDeviceId = deviceId,
				AuthorizedUsersId = userId
			});
		}

		return requestedDeviceIds;
	}

	/// <summary>
	/// 更新 API 凭据授权设备列表。
	/// </summary>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="owner">API 凭据所属用户摘要</param>
	/// <param name="authorizedDeviceIds">新的授权设备 ID 列表</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	private async Task UpdateApiCredentialDeviceLinksAsync(
		Guid apiCredentialId,
		ApiCredentialOwner owner,
		IReadOnlyCollection<Guid>? authorizedDeviceIds,
		bool isNewApiCredential,
		CancellationToken cancellationToken) {
		// 规范化授权设备 ID 列表，去除 Guid.Empty
		var requestedDeviceIds = authorizedDeviceIds?
			.Where(deviceId => deviceId != Guid.Empty)
			.ToHashSet() ?? [];

		// 取出当前 API 凭据授权设备 ID 列表
		var currentDeviceIds = isNewApiCredential
			? []
			: await dbContext.ApiCredentialDevices
			.AsNoTracking()
			.Where(authorization => authorization.AuthorizedApiCredentialsApiCredentialId == apiCredentialId)
			.Select(authorization => authorization.AuthorizedDevicesDeviceId)
			.ToHashSetAsync(cancellationToken);

		// 计算需要移除和新增的授权设备 ID
		var removedDeviceIds = currentDeviceIds.Except(requestedDeviceIds).ToHashSet();
		var addedDeviceIds = requestedDeviceIds.Except(currentDeviceIds).ToHashSet();

		// 先校验新增授权设备完整存在且没有超过所属用户范围，再更新中间表
		if (addedDeviceIds.Count > 0) {
			var devicesQuery = dbContext.Devices
				.AsNoTracking()
				.Where(device => addedDeviceIds.Contains(device.DeviceId));
			if (owner.Role == PrincipalRole.LimitedQuery) {
				// LimitedQuery 用户创建的凭据只能授权该用户自己已经可访问的设备
				devicesQuery = devicesQuery.Where(device => device.AuthorizedUsers.Any(user => user.Id == owner.UserId));
			}

			var existingAddedDeviceCount = await devicesQuery.CountAsync(cancellationToken);
			if (existingAddedDeviceCount != addedDeviceIds.Count) {
				throw new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, "API 凭据授权设备范围包含不存在或所属用户不可访问的设备");
			}
		}

		// 先移除被裁剪的授权设备
		if (removedDeviceIds.Count > 0) {
			var removedAuthorizations = await dbContext.ApiCredentialDevices
				.Where(authorization => authorization.AuthorizedApiCredentialsApiCredentialId == apiCredentialId
					&& removedDeviceIds.Contains(authorization.AuthorizedDevicesDeviceId))
				.ToListAsync(cancellationToken);
			dbContext.ApiCredentialDevices.RemoveRange(removedAuthorizations);
		}

		// 再新增新的授权设备
		foreach (var addedDeviceId in addedDeviceIds) {
			dbContext.ApiCredentialDevices.Add(new ApiCredentialDevice {
				AuthorizedApiCredentialsApiCredentialId = apiCredentialId,
				AuthorizedDevicesDeviceId = addedDeviceId
			});
		}
	}
}