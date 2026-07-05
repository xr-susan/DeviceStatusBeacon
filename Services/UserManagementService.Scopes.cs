namespace DeviceStatusBeacon.Services;

public sealed partial class UserManagementService {
	/// <summary>
	/// 收窄关联指定用户的 API 凭据权限范围以匹配用户的新角色。
	/// </summary>
	/// <remarks>
	/// 此重载在用户新角色为 <see cref="PrincipalRole.LimitedQuery"/> 时会自动查询用户授权的设备 ID 列表，并在后续裁剪 API 凭据的授权设备范围。
	/// </remarks>
	/// <param name="userId">用户 ID</param>
	/// <param name="userNewRole">用户的新角色</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <param name="saveChanges">是否在方法内部保存变更</param>
	/// <returns>一个表示异步操作的任务</returns>
	private async Task ShrinkApiCredentialScopesAsync(Guid userId, PrincipalRole userNewRole, CancellationToken cancellationToken, bool saveChanges = true) {
		var authorizedDeviceIdsByUser = userNewRole == PrincipalRole.LimitedQuery
			? await GetAuthorizedDeviceIdsByUserAsync([userId], cancellationToken)
			: [];

		await ShrinkApiCredentialScopesAsync(
			new Dictionary<Guid, PrincipalRole> { [userId] = userNewRole },
			authorizedDeviceIdsByUser,
			cancellationToken,
			saveChanges);
	}

	/// <summary>
	/// 收窄关联指定用户的 API 凭据权限范围以匹配用户的新角色和设备授权范围。
	/// </summary>
	/// <param name="userId">用户 ID</param>
	/// <param name="userNewRole">用户的新角色</param>
	/// <param name="userAuthorizedDeviceIds">用户新的授权设备 ID 列表；仅在用户为 <see cref="PrincipalRole.LimitedQuery"/> 时使用</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <param name="saveChanges">是否在方法内部保存变更</param>
	/// <returns>一个表示异步操作的任务</returns>
	private async Task ShrinkApiCredentialScopesAsync(Guid userId, PrincipalRole userNewRole, HashSet<Guid>? userAuthorizedDeviceIds, CancellationToken cancellationToken, bool saveChanges = true) {
		Dictionary<Guid, HashSet<Guid>> authorizedDeviceIdsByUser = [];

		if (userNewRole == PrincipalRole.LimitedQuery) {
			if (userAuthorizedDeviceIds is null) {
				throw new InvalidOperationException("用户新角色为 LimitedQuery 时必须提供新的授权设备 ID 列表");
			}

			authorizedDeviceIdsByUser[userId] = userAuthorizedDeviceIds;
		}

		await ShrinkApiCredentialScopesAsync(
			new Dictionary<Guid, PrincipalRole> { [userId] = userNewRole },
			authorizedDeviceIdsByUser,
			cancellationToken,
			saveChanges);
	}

	/// <summary>
	/// 批量收窄关联指定用户集合的 API 凭据权限范围。
	/// </summary>
	/// <param name="userNewRoles">用户 ID 到新角色的映射</param>
	/// <param name="authorizedDeviceIdsByLimitedQueryUser"><see cref="PrincipalRole.LimitedQuery"/> 用户 ID 到新授权设备 ID 集合的映射</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <param name="saveChanges">是否在方法内部保存变更</param>
	/// <returns>一个表示异步操作的任务</returns>
	private async Task ShrinkApiCredentialScopesAsync(
		Dictionary<Guid, PrincipalRole> userNewRoles,
		Dictionary<Guid, HashSet<Guid>> authorizedDeviceIdsByLimitedQueryUser,
		CancellationToken cancellationToken,
		bool saveChanges = true) {
		if (userNewRoles.Count == 0) {
			return;
		}

		var userIds = userNewRoles.Keys.ToHashSet();

		// 先加载凭据实体本身，因为角色降级需要由 change tracker 保存
		var credentials = await dbContext.ApiCredentials
			.Where(credential => userIds.Contains(credential.UserId))
			.ToListAsync(cancellationToken);

		foreach (var credential in credentials) {
			var userNewRole = userNewRoles[credential.UserId];
			if (credential.Role > userNewRole) {
				// 凭据角色不得高于所属用户角色，降级用户时同步裁剪凭据角色
				credential.Role = userNewRole;
			}
		}

		// 只有 LimitedQuery 用户的凭据授权设备需要受用户设备列表约束
		// 在内存中进一步取出 LimitedQuery 用户的凭据，避免在数据库中多次 join User/Role 表
		var limitedQueryCredentialOwners = credentials
			.Where(credential => userNewRoles[credential.UserId] == PrincipalRole.LimitedQuery)
			.ToDictionary(credential => credential.ApiCredentialId, credential => credential.UserId);

		if (limitedQueryCredentialOwners.Count > 0) {
			// 取出所有需要被裁剪的 ApiCredentialId
			var limitedQueryCredentialIds = limitedQueryCredentialOwners.Keys.ToHashSet();

			// 通过需要被裁剪的凭据 ID 查询其授权列表
			var credentialAuthorizations = await dbContext.ApiCredentialDevices
				.Where(authorization => limitedQueryCredentialIds.Contains(authorization.AuthorizedApiCredentialsApiCredentialId))
				.ToListAsync(cancellationToken);

			foreach (var authorization in credentialAuthorizations) {
				// 根据凭据 ID 找到所属的 LimitedQuery 用户 ID
				var ownerUserId = limitedQueryCredentialOwners[authorization.AuthorizedApiCredentialsApiCredentialId];

				// 根据所属用户 ID 找到新的授权设备 ID 列表
				if (!authorizedDeviceIdsByLimitedQueryUser.TryGetValue(ownerUserId, out var authorizedDeviceIds)) {
					throw new InvalidOperationException("LimitedQuery 用户必须提供新的授权设备 ID 列表");
				}

				// 如果凭据授权的设备不在用户新的授权设备列表中，则删除该授权
				if (!authorizedDeviceIds.Contains(authorization.AuthorizedDevicesDeviceId)) {
					// 直接删除 ApiCredentialDevice 行，比加载完整 AuthorizedDevices 导航集合更清晰
					dbContext.ApiCredentialDevices.Remove(authorization);
				}
			}
		}

		if (saveChanges) {
			await dbContext.SaveChangesAsync(cancellationToken);
		}
	}

	/// <summary>
	/// 按用户批量读取授权设备 ID 集合。
	/// </summary>
	/// <param name="userIds">用户 ID 列表</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>用户 ID 到授权设备 ID 集合的映射；没有授权设备的用户对应空集合</returns>
	private async Task<Dictionary<Guid, HashSet<Guid>>> GetAuthorizedDeviceIdsByUserAsync(List<Guid> userIds, CancellationToken cancellationToken) {
		if (userIds.Count == 0) {
			return [];
		}

		var result = userIds.ToDictionary(userId => userId, _ => new HashSet<Guid>());

		// 直接读取显式 DeviceUser 中间表，避免经由 Device/User 导航生成更复杂的查询
		var authorizationRows = await dbContext.DeviceUsers
			.AsNoTracking()
			.Where(authorization => userIds.Contains(authorization.AuthorizedUsersId))
			.Select(authorization => new {
				UserId = authorization.AuthorizedUsersId,
				DeviceId = authorization.AuthorizedDevicesDeviceId
			})
			.ToListAsync(cancellationToken);

		foreach (var authorizationRow in authorizationRows) {
			// 数据库查询只在循环前执行，循环内只把结果合并到内存字典
			result[authorizationRow.UserId].Add(authorizationRow.DeviceId);
		}

		return result;
	}
}