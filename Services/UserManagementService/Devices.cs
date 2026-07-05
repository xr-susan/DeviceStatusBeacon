namespace DeviceStatusBeacon.Services;

public sealed partial class UserManagementService {
	/// <inheritdoc/>
	public async Task SetDeviceAuthorizedUsersAsync(Guid deviceId, SetDeviceAuthorizedUsersCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		await SetDeviceAuthorizedUsersAsync(
			dbContext.Devices.WhereDeviceId(deviceId),
			command,
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task SetDeviceAuthorizedUsersAsync(string deviceName, SetDeviceAuthorizedUsersCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		var deviceNameLookup = CreateDeviceNameLookup(deviceName);
		await SetDeviceAuthorizedUsersAsync(
			dbContext.Devices.WhereDeviceName(deviceNameLookup),
			command,
			cancellationToken);
	}

	/// <summary>
	/// 更新指定设备的授权用户范围，并在需要时收窄被移除用户的 API 凭据权限范围。
	/// </summary>
	/// <param name="devices">已经限定目标设备范围的设备查询</param>
	/// <param name="command">设备授权用户更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	private async Task SetDeviceAuthorizedUsersAsync(IQueryable<Device> devices, SetDeviceAuthorizedUsersCommand command, CancellationToken cancellationToken) {
		// 设备视角的授权用户列表是访问控制配置，更新后需要同步收缩被移除 LimitedQuery 用户的凭据范围
		// 关系替换和凭据收缩必须处于同一个事务，避免管理操作失败时只落下一半访问控制状态
		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

		// 只读取目标设备 ID，授权关系后续直接通过 DeviceUser 中间表处理
		var device = await devices
			.Select(entity => new { entity.DeviceId })
			.SingleOrDefaultAsync(cancellationToken)
			?? throw new UserManagementCommandException(StatusCodes.Status404NotFound, "未找到指定的设备");
		var deviceId = device.DeviceId;

		var requestedUserIds = command.AuthorizedUserIds
			.Where(userId => userId != Guid.Empty)
			.ToHashSet();

		// 先校验全部目标用户存在，再写入中间表，避免失败后污染当前 DbContext 跟踪状态
		// 空 Guid 视为无效输入项并忽略，保持与其他授权列表命令的规范化语义一致
		if (requestedUserIds.Count > 0) {
			var existingUserCount = await dbContext.Users
				.AsNoTracking()
				.CountAsync(user => requestedUserIds.Contains(user.Id), cancellationToken);
			if (existingUserCount != requestedUserIds.Count) {
				throw new UserManagementCommandException(StatusCodes.Status422UnprocessableEntity, "授权用户范围包含不存在的用户");
			}
		}

		// 显式读取当前 DeviceUser 关系，分别计算新增和移除，避免 Clear 后重建全部关系
		var currentAuthorizedUserIds = await dbContext.DeviceUsers
			.AsNoTracking()
			.Where(authorization => authorization.AuthorizedDevicesDeviceId == deviceId)
			.Select(authorization => authorization.AuthorizedUsersId)
			.ToHashSetAsync(cancellationToken);

		var removedUserIds = currentAuthorizedUserIds.Except(requestedUserIds).ToHashSet();
		var addedUserIds = requestedUserIds.Except(currentAuthorizedUserIds).ToHashSet();

		// 先删除被移除的授权关系
		if (removedUserIds.Count > 0) {
			var removedAuthorizations = await dbContext.DeviceUsers
				.Where(authorization => authorization.AuthorizedDevicesDeviceId == deviceId
					&& removedUserIds.Contains(authorization.AuthorizedUsersId))
				.ToListAsync(cancellationToken);
			dbContext.DeviceUsers.RemoveRange(removedAuthorizations);
		}

		// 再新增新的授权关系
		foreach (var addedUserId in addedUserIds) {
			dbContext.DeviceUsers.Add(new DeviceUser {
				AuthorizedDevicesDeviceId = deviceId,
				AuthorizedUsersId = addedUserId
			});
		}

		// 如果有新增或移除的用户，保存变更后再收窄被移除用户的 API 凭据范围
		if (removedUserIds.Count > 0 || addedUserIds.Count > 0) {
			await dbContext.SaveChangesAsync(cancellationToken);
		}

		if (removedUserIds.Count > 0) {
			// 新增用户的可见范围会由新的 DeviceUser 关系自然提供，只有被移除用户可能留下过宽的 API 凭据授权
			await ShrinkRemovedLimitedQueryUsersAsync(removedUserIds, cancellationToken);
		}

		await transaction.CommitAsync(cancellationToken);
	}

	/// <summary>
	/// 收窄从设备授权列表中移除的有限查询用户的 API 凭据权限范围。
	/// </summary>
	/// <param name="removedUserIds">被移除的用户 ID 列表</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	private async Task ShrinkRemovedLimitedQueryUsersAsync(IReadOnlyCollection<Guid> removedUserIds, CancellationToken cancellationToken) {
		// 先读取被移除用户的角色，只有 LimitedQuery 用户的访问范围受 AuthorizedDevices 限制
		// 其他角色的设备授权列表只是预置配置，不应因为从单个设备移除就裁剪其凭据
		var removedUsers = await dbContext.Users
			.AsNoTracking()
			.Where(user => removedUserIds.Contains(user.Id))
			.Select(user => new {
				user.Id,
				RoleName = user.UserRoles.Select(userRole => userRole.Role.Name).SingleOrDefault()
			})
			.ToListAsync(cancellationToken);

		List<Guid> limitedQueryUserIds = [];
		foreach (var removedUser in removedUsers) {
			if (!PrincipalRole.TryParse(removedUser.RoleName, out var role)) {
				throw new UserManagementCommandException(StatusCodes.Status409Conflict, "被移除的授权用户未正确设置角色");
			}

			if (role != PrincipalRole.LimitedQuery) {
				// 非 LimitedQuery 用户当前不受授权设备列表限制，后续降级时会由降级流程重新收缩
				continue;
			}

			limitedQueryUserIds.Add(removedUser.Id);
		}

		if (limitedQueryUserIds.Count == 0) {
			// 没有受设备列表限制的用户，后续凭据范围不会因为这次关系变更而变窄
			return;
		}

		// 上游已经保存 DeviceUser 变更，这里读取到的是移除后的剩余授权设备
		var authorizedDeviceIdsByUser = await GetAuthorizedDeviceIdsByUserAsync(limitedQueryUserIds, cancellationToken);
		var newRolesByUser = limitedQueryUserIds.ToDictionary(userId => userId, _ => PrincipalRole.LimitedQuery);

		// 多个被移除用户共享同一个外层事务，凭据裁剪统一保存，避免每个用户各写一次数据库
		await ShrinkApiCredentialScopesAsync(newRolesByUser, authorizedDeviceIdsByUser, cancellationToken);
	}
}