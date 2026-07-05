using Microsoft.Data.Sqlite;

namespace DeviceStatusBeacon.Services;

public sealed partial class DeviceManagementService {
	/// <inheritdoc/>
	public async Task<CreateDeviceCommandResult> CreateAsync(CreateDeviceCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new DeviceManagementCommandException(StatusCodes.Status422UnprocessableEntity, message));

		EnsureValidDeviceName(command.DeviceName, "设备名称不符合身份标识格式");

		var unprotectedSecretKey = ISecurityServiceV1.GenerateRandomBytes();
		var newDevice = new Device {
			DeviceName = command.DeviceName,
			NormalizedDeviceName = IdentityNameLookup.CreateFromValidName(command.DeviceName, lookupNormalizer).NormalizedName,
			DisplayName = command.DisplayName,
			ProtectedSecretKey = dataProtector.ProtectKey(unprotectedSecretKey)
		};

		try {
			dbContext.Devices.Add(newDevice);
			await dbContext.SaveChangesAsync(cancellationToken);
		} catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 }) {
			throw new DeviceManagementCommandException(StatusCodes.Status409Conflict, "指定的设备名称已被使用");
		}

		return new(
			newDevice.DeviceId,
			newDevice.DeviceName,
			newDevice.DisplayName,
			Convert.ToBase64String(unprotectedSecretKey));
	}

	/// <inheritdoc/>
	public async Task RenameAsync(Guid deviceId, RenameDeviceCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new DeviceManagementCommandException(StatusCodes.Status422UnprocessableEntity, message));

		EnsureValidDeviceName(command.NewDeviceName, "新设备名称不符合身份标识格式");

		await RenameAsync(
			dbContext.Devices.WhereDeviceId(deviceId),
			command.NewDeviceName,
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task RenameAsync(string oldDeviceName, RenameDeviceCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new DeviceManagementCommandException(StatusCodes.Status422UnprocessableEntity, message));

		EnsureValidDeviceName(command.NewDeviceName, "新设备名称不符合身份标识格式");

		var oldDeviceNameLookup = CreateDeviceNameLookup(oldDeviceName);

		await RenameAsync(
			dbContext.Devices.WhereDeviceName(oldDeviceNameLookup),
			command.NewDeviceName,
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task DeleteAsync(Guid deviceId, CancellationToken cancellationToken = default) =>
		await DeleteAsync(
			dbContext.Devices.WhereDeviceId(deviceId),
			cancellationToken);

	/// <inheritdoc/>
	public async Task DeleteAsync(string deviceName, CancellationToken cancellationToken = default) {
		var deviceNameLookup = CreateDeviceNameLookup(deviceName);

		await DeleteAsync(
			dbContext.Devices.WhereDeviceName(deviceNameLookup),
			cancellationToken);
	}

	/// <summary>
	/// 重命名指定设备。
	/// </summary>
	/// <param name="devices">已经限定目标设备范围的设备查询</param>
	/// <param name="newDeviceName">新设备名称</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	private async Task RenameAsync(IQueryable<Device> devices, string newDeviceName, CancellationToken cancellationToken) {
		var newDeviceNameLookup = IdentityNameLookup.CreateFromValidName(newDeviceName, lookupNormalizer);

		try {
			var updatedCount = await devices.ExecuteUpdateAsync(
				device => device
					.SetProperty(entity => entity.DeviceName, newDeviceName)
					.SetProperty(entity => entity.NormalizedDeviceName, newDeviceNameLookup.NormalizedName),
				cancellationToken);

			EnsureDeviceFound(updatedCount);
		} catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 }) {
			throw new DeviceManagementCommandException(StatusCodes.Status409Conflict, "指定的新设备名称已被使用");
		}
	}

	/// <summary>
	/// 删除指定设备。
	/// </summary>
	/// <param name="devices">已经限定目标设备范围的设备查询</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	private async Task DeleteAsync(IQueryable<Device> devices, CancellationToken cancellationToken) {
		var recentLogBoundary = DateTime.UtcNow.Subtract(IDeviceManagementService.DeleteRecentLogBlockWindow);
		var deleteQuery = devices
			.Where(device => !device.Enabled)
			.Where(device => !dbContext.OnlineLogs.Any(log =>
				log.DeviceId == device.DeviceId
				&& log.LogTime >= recentLogBoundary));

		// 尝试直接删除符合条件的设备
		var deletedCount = await deleteQuery.ExecuteDeleteAsync(cancellationToken);
		if (deletedCount > 0) {
			return;
		}

		// 如果没有设备被删除，则需要进一步检查原因，可能是设备不存在、设备未停用或设备近7天有新日志
		var target = await devices
			.AsNoTracking()
			.Select(device => new DeviceManagementTarget(
				device.DeviceId,
				device.Enabled))
			.SingleOrDefaultAsync(cancellationToken)
			?? throw new DeviceManagementCommandException(StatusCodes.Status404NotFound, "未找到指定的设备");

		if (target.Enabled) {
			throw new DeviceManagementCommandException(StatusCodes.Status409Conflict, "设备必须先停用后才能删除");
		}

		var hasRecentLog = await dbContext.OnlineLogs
			.AsNoTracking()
			.AnyAsync(log =>
				log.DeviceId == target.DeviceId
				&& log.LogTime >= recentLogBoundary,
				cancellationToken);
		if (hasRecentLog) {
			throw new DeviceManagementCommandException(StatusCodes.Status409Conflict, $"设备近{IDeviceManagementService.DeleteRecentLogBlockWindow.TotalDays}天存在新日志，暂不能删除");
		}

		// 如果仍然没有设备被删除，则说明存在并发问题，此时再次尝试删除设备
		deletedCount = await deleteQuery.ExecuteDeleteAsync(cancellationToken);
		if (deletedCount > 0) {
			return;
		}

		// 如果仍然没有设备被删除，则说明存在并发问题，此时抛出异常提示用户重试
		throw new DeviceManagementCommandException(StatusCodes.Status409Conflict, "设备删除失败，请重试");
	}
}