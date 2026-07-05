namespace DeviceStatusBeacon.Services;

public sealed partial class DeviceAdministrationService {
	/// <inheritdoc/>
	public async Task<ResetDeviceSecretKeyCommandResult> ResetSecretKeyAsync(Guid deviceId, CancellationToken cancellationToken = default) =>
		await ResetSecretKeyAsync(
			dbContext.Devices.WhereDeviceId(deviceId),
			cancellationToken);

	/// <inheritdoc/>
	public async Task<ResetDeviceSecretKeyCommandResult> ResetSecretKeyAsync(string deviceName, CancellationToken cancellationToken = default) {
		var deviceNameLookup = CreateDeviceNameLookup(deviceName);

		return await ResetSecretKeyAsync(
			dbContext.Devices.WhereDeviceName(deviceNameLookup),
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task SetDisplayNameAsync(Guid deviceId, SetDeviceDisplayNameCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new DeviceAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		await SetDisplayNameAsync(
			dbContext.Devices.WhereDeviceId(deviceId),
			command.DisplayName,
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task SetDisplayNameAsync(string deviceName, SetDeviceDisplayNameCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new DeviceAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		var deviceNameLookup = CreateDeviceNameLookup(deviceName);

		await SetDisplayNameAsync(
			dbContext.Devices.WhereDeviceName(deviceNameLookup),
			command.DisplayName,
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task SetEnabledAsync(Guid deviceId, SetDeviceEnabledCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new DeviceAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		await SetEnabledAsync(
			dbContext.Devices.WhereDeviceId(deviceId),
			command.Enabled,
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task SetEnabledAsync(string deviceName, SetDeviceEnabledCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new DeviceAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		var deviceNameLookup = CreateDeviceNameLookup(deviceName);

		await SetEnabledAsync(
			dbContext.Devices.WhereDeviceName(deviceNameLookup),
			command.Enabled,
			cancellationToken);
	}

	/// <summary>
	/// 重置指定设备的操作密钥。
	/// </summary>
	/// <param name="devices">已经限定目标设备范围的设备查询</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为密钥重置结果</returns>
	private async Task<ResetDeviceSecretKeyCommandResult> ResetSecretKeyAsync(IQueryable<Device> devices, CancellationToken cancellationToken) {
		var unprotectedSecretKey = ISecurityServiceV1.GenerateRandomBytes();
		var protectedSecretKey = dataProtector.ProtectKey(unprotectedSecretKey);

		var updatedCount = await devices.ExecuteUpdateAsync(
			device => device.SetProperty(entity => entity.ProtectedSecretKey, protectedSecretKey),
			cancellationToken);

		EnsureDeviceFound(updatedCount);

		return new(Convert.ToBase64String(unprotectedSecretKey));
	}

	/// <summary>
	/// 更新指定设备的显示名称。
	/// </summary>
	/// <param name="devices">已经限定目标设备范围的设备查询</param>
	/// <param name="displayName">新的显示名称</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	private static async Task SetDisplayNameAsync(IQueryable<Device> devices, string? displayName, CancellationToken cancellationToken) {
		var updatedCount = await devices.ExecuteUpdateAsync(
			device => device.SetProperty(entity => entity.DisplayName, displayName),
			cancellationToken);

		EnsureDeviceFound(updatedCount);
	}

	/// <summary>
	/// 更新指定设备的启用状态。
	/// </summary>
	/// <param name="devices">已经限定目标设备范围的设备查询</param>
	/// <param name="enabled">是否启用设备</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	private static async Task SetEnabledAsync(IQueryable<Device> devices, bool enabled, CancellationToken cancellationToken) {
		var updatedCount = await devices.ExecuteUpdateAsync(
			device => device.SetProperty(entity => entity.Enabled, enabled),
			cancellationToken);

		EnsureDeviceFound(updatedCount);
	}
}