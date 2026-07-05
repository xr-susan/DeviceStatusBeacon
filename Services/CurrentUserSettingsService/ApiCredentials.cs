using System.Security.Claims;

namespace DeviceStatusBeacon.Services;

public sealed partial class CurrentUserSettingsService {
	/// <inheritdoc/>
	public async Task<CreateApiCredentialCommandResult> CreateApiCredentialAsync(ClaimsPrincipal principal, CreateApiCredentialCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		var target = await GetCurrentUserTargetAsync(principal, cancellationToken);
		try {
			// 当前用户入口只负责归属限定，个人凭据创建复用管理员服务
			return await userManagementService.CreateApiCredentialAsync(target.UserId, command, cancellationToken);
		} catch (UserManagementCommandException e) {
			throw ToCurrentUserSettingsCommandException(e);
		}
	}

	/// <inheritdoc/>
	public async Task SetApiCredentialDisplayNameAsync(ClaimsPrincipal principal, Guid apiCredentialId, SetApiCredentialDisplayNameCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		await GetCurrentUserTargetAsync(principal, cancellationToken, apiCredentialId);
		try {
			// 当前用户入口只负责归属校验，显示名称更新复用管理员服务
			await userManagementService.SetApiCredentialDisplayNameAsync(apiCredentialId, command, cancellationToken);
		} catch (UserManagementCommandException e) {
			throw ToCurrentUserSettingsCommandException(e);
		}
	}

	/// <inheritdoc/>
	public async Task SetApiCredentialEnabledAsync(ClaimsPrincipal principal, Guid apiCredentialId, SetApiCredentialEnabledCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		await GetCurrentUserTargetAsync(principal, cancellationToken, apiCredentialId);
		try {
			// 当前用户入口只负责归属校验，启用状态更新复用管理员服务
			await userManagementService.SetApiCredentialEnabledAsync(apiCredentialId, command, cancellationToken);
		} catch (UserManagementCommandException e) {
			throw ToCurrentUserSettingsCommandException(e);
		}
	}

	/// <inheritdoc/>
	public async Task SetApiCredentialRoleAsync(ClaimsPrincipal principal, Guid apiCredentialId, SetApiCredentialRoleCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		await GetCurrentUserTargetAsync(principal, cancellationToken, apiCredentialId);
		try {
			// 当前用户入口只负责归属校验，凭据角色更新复用管理员服务
			await userManagementService.SetApiCredentialRoleAsync(apiCredentialId, command, cancellationToken);
		} catch (UserManagementCommandException e) {
			throw ToCurrentUserSettingsCommandException(e);
		}
	}

	/// <inheritdoc/>
	public async Task SetApiCredentialAuthorizedDevicesAsync(ClaimsPrincipal principal, Guid apiCredentialId, SetApiCredentialAuthorizedDevicesCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		await GetCurrentUserTargetAsync(principal, cancellationToken, apiCredentialId);
		try {
			// 当前用户入口只负责归属校验，授权设备更新复用管理员服务
			await userManagementService.SetApiCredentialAuthorizedDevicesAsync(apiCredentialId, command, cancellationToken);
		} catch (UserManagementCommandException e) {
			throw ToCurrentUserSettingsCommandException(e);
		}
	}

	/// <inheritdoc/>
	public async Task<ResetApiCredentialSecretKeyCommandResult> ResetApiCredentialSecretKeyAsync(ClaimsPrincipal principal, Guid apiCredentialId, CancellationToken cancellationToken = default) {
		await GetCurrentUserTargetAsync(principal, cancellationToken, apiCredentialId);
		try {
			// 当前用户入口只负责归属校验，凭据密钥重置复用管理员服务
			return await userManagementService.ResetApiCredentialSecretKeyAsync(apiCredentialId, cancellationToken);
		} catch (UserManagementCommandException e) {
			throw ToCurrentUserSettingsCommandException(e);
		}
	}

	/// <inheritdoc/>
	public async Task DeleteApiCredentialAsync(ClaimsPrincipal principal, Guid apiCredentialId, CancellationToken cancellationToken = default) {
		await GetCurrentUserTargetAsync(principal, cancellationToken, apiCredentialId);
		try {
			// 当前用户入口只负责归属校验，删除凭据操作复用管理员服务
			await userManagementService.DeleteApiCredentialAsync(apiCredentialId, cancellationToken);
		} catch (UserManagementCommandException e) {
			throw ToCurrentUserSettingsCommandException(e);
		}
	}
}