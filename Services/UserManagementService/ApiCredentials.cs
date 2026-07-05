using Microsoft.Data.Sqlite;

namespace DeviceStatusBeacon.Services;

public sealed partial class UserManagementService {
	/// <inheritdoc/>
	public async Task<CreateApiCredentialCommandResult> CreateApiCredentialAsync(Guid ownerUserId, CreateApiCredentialCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		// 先读取所属用户角色，再创建凭据，确保凭据角色不会高于所属用户
		var owner = await GetApiCredentialOwnerAsync(
			dbContext.Users.Where(user => user.Id == ownerUserId),
			"未找到指定的所属用户",
			"所属用户未正确设置角色",
			cancellationToken);
		return await CreateApiCredentialAsync(owner, command, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<CreateApiCredentialCommandResult> CreateApiCredentialAsync(string ownerUserName, CreateApiCredentialCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		// 用户名入口先归一化再查找，保持与 Identity 用户名唯一索引一致的匹配语义
		var ownerNameLookup = CreateUserNameLookup(ownerUserName);
		var owner = await GetApiCredentialOwnerAsync(
			dbContext.Users.Where(user => user.NormalizedUserName == ownerNameLookup.NormalizedName),
			"未找到指定的所属用户",
			"所属用户未正确设置角色",
			cancellationToken);
		return await CreateApiCredentialAsync(owner, command, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task SetApiCredentialDisplayNameAsync(Guid apiCredentialId, SetApiCredentialDisplayNameCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		try {
			// 显示名称受同一用户下唯一索引保护，使用批量更新后把 SQLite 唯一约束冲突转成业务异常
			var updatedCount = await dbContext.ApiCredentials
				.WhereApiCredentialId(apiCredentialId)
				.ExecuteUpdateAsync(
					credential => credential.SetProperty(entity => entity.DisplayName, command.DisplayName),
					cancellationToken);

			EnsureEntityFound(updatedCount, "未找到指定的 API 凭据");
		} catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 }) {
			throw new UserManagementCommandException(StatusCodes.Status409Conflict, "指定的 API 凭据显示名称在关联用户下已被使用");
		}
	}

	/// <inheritdoc/>
	public async Task SetApiCredentialEnabledAsync(Guid apiCredentialId, SetApiCredentialEnabledCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		// 启用状态不涉及导航属性，直接批量更新即可
		var updatedCount = await dbContext.ApiCredentials
			.WhereApiCredentialId(apiCredentialId)
			.ExecuteUpdateAsync(
				credential => credential.SetProperty(entity => entity.Enabled, command.Enabled),
				cancellationToken);

		EnsureEntityFound(updatedCount, "未找到指定的 API 凭据");
	}

	/// <inheritdoc/>
	public async Task SetApiCredentialRoleAsync(Guid apiCredentialId, SetApiCredentialRoleCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		var target = await GetApiCredentialTargetAsync(apiCredentialId, cancellationToken);
		EnsureApiCredentialRoleWithinOwnerRole(command.Role, target.Owner.Role);

		var updatedCount = await dbContext.ApiCredentials
			.WhereApiCredentialId(apiCredentialId)
			.ExecuteUpdateAsync(
				credential => credential.SetProperty(entity => entity.Role, command.Role),
				cancellationToken);

		EnsureEntityFound(updatedCount, "未找到指定的 API 凭据");
	}

	/// <inheritdoc/>
	public async Task SetApiCredentialAuthorizedDevicesAsync(Guid apiCredentialId, SetApiCredentialAuthorizedDevicesCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		var target = await GetApiCredentialTargetAsync(apiCredentialId, cancellationToken);
		EnsureApiCredentialRoleWithinOwnerRole(target.Role, target.Owner.Role);

		await UpdateApiCredentialDeviceLinksAsync(target.ApiCredentialId, target.Owner, command.AuthorizedDeviceIds, false, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<ResetApiCredentialSecretKeyCommandResult> ResetApiCredentialSecretKeyAsync(Guid apiCredentialId, CancellationToken cancellationToken = default) {
		// 未保护的操作密钥只在本次返回
		var unprotectedSecretKey = ISecurityServiceV1.GenerateRandomBytes();
		var protectedSecretKey = dataProtector.ProtectKey(unprotectedSecretKey);

		var updatedCount = await dbContext.ApiCredentials
			.WhereApiCredentialId(apiCredentialId)
			.ExecuteUpdateAsync(
				credential => credential.SetProperty(entity => entity.ProtectedSecretKey, protectedSecretKey),
				cancellationToken);

		EnsureEntityFound(updatedCount, "未找到指定的 API 凭据");

		return new(Convert.ToBase64String(unprotectedSecretKey));
	}

	/// <inheritdoc/>
	public async Task DeleteApiCredentialAsync(Guid apiCredentialId, CancellationToken cancellationToken = default) {
		var deletedCount = await dbContext.ApiCredentials
			.WhereApiCredentialId(apiCredentialId)
			.ExecuteDeleteAsync(cancellationToken);

		EnsureEntityFound(deletedCount, "未找到指定的 API 凭据");
	}

	/// <summary>
	/// 创建 API 凭据。
	/// </summary>
	/// <param name="owner">API 凭据所属用户摘要</param>
	/// <param name="command">API 凭据创建请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为 API 凭据创建结果</returns>
	private async Task<CreateApiCredentialCommandResult> CreateApiCredentialAsync(ApiCredentialOwner owner, CreateApiCredentialCommand command, CancellationToken cancellationToken) {
		EnsureApiCredentialRoleWithinOwnerRole(command.Role, owner.Role);

		// API 凭据密钥创建结果必须一次性返回明文，后续只能重置
		var unprotectedSecretKey = ISecurityServiceV1.GenerateRandomBytes();
		var newCredential = new ApiCredential {
			UserId = owner.UserId,
			DisplayName = string.IsNullOrWhiteSpace(command.DisplayName) ? null : command.DisplayName,
			ProtectedSecretKey = dataProtector.ProtectKey(unprotectedSecretKey),
			Role = command.Role
		};

		try {
			dbContext.ApiCredentials.Add(newCredential);
			await UpdateApiCredentialDeviceLinksAsync(newCredential.ApiCredentialId, owner, command.AuthorizedDeviceIds, true, cancellationToken);
			await dbContext.SaveChangesAsync(cancellationToken);
		} catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 }) {
			throw new UserManagementCommandException(StatusCodes.Status409Conflict, "指定的 API 凭据显示名称在关联用户下已被使用");
		}

		return new(
			newCredential.ApiCredentialId,
			Convert.ToBase64String(unprotectedSecretKey));
	}
}