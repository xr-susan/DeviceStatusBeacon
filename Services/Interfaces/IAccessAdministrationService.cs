namespace DeviceStatusBeacon.Services.Interfaces;

/// <summary>
/// 用户与 API 凭据管理服务。
/// </summary>
public interface IAccessAdministrationService {
	/// <summary>
	/// 创建新用户。
	/// </summary>
	/// <param name="command">用户创建请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为用户创建结果</returns>
	Task<CreateUserCommandResult> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 重命名指定用户。
	/// </summary>
	/// <param name="userId">用户 ID</param>
	/// <param name="command">用户重命名请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task RenameUserAsync(Guid userId, RenameUserCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 重命名指定用户。
	/// </summary>
	/// <param name="userName">用户名</param>
	/// <param name="command">用户重命名请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task RenameUserAsync(string userName, RenameUserCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定用户的显示名称。
	/// </summary>
	/// <param name="userId">用户 ID</param>
	/// <param name="command">用户显示名称更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetUserDisplayNameAsync(Guid userId, SetUserDisplayNameCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定用户的显示名称。
	/// </summary>
	/// <param name="userName">用户名</param>
	/// <param name="command">用户显示名称更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetUserDisplayNameAsync(string userName, SetUserDisplayNameCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 重置指定用户的密码。
	/// </summary>
	/// <param name="userId">用户 ID</param>
	/// <param name="command">用户密码重置请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task ResetUserPasswordAsync(Guid userId, ResetUserPasswordCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 重置指定用户的密码。
	/// </summary>
	/// <param name="userName">用户名</param>
	/// <param name="command">用户密码重置请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task ResetUserPasswordAsync(string userName, ResetUserPasswordCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定用户的角色，并在需要时收窄其 API 凭据权限范围。
	/// </summary>
	/// <param name="userId">用户 ID</param>
	/// <param name="command">用户角色更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetUserRoleAsync(Guid userId, SetUserRoleCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定用户的角色，并在需要时收窄其 API 凭据权限范围。
	/// </summary>
	/// <param name="userName">用户名</param>
	/// <param name="command">用户角色更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetUserRoleAsync(string userName, SetUserRoleCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定用户的授权设备范围，并在需要时收窄其 API 凭据权限范围。
	/// </summary>
	/// <param name="userId">用户 ID</param>
	/// <param name="command">用户授权设备更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetUserAuthorizedDevicesAsync(Guid userId, SetUserAuthorizedDevicesCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定用户的授权设备范围，并在需要时收窄其 API 凭据权限范围。
	/// </summary>
	/// <param name="userName">用户名</param>
	/// <param name="command">用户授权设备更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetUserAuthorizedDevicesAsync(string userName, SetUserAuthorizedDevicesCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 删除指定用户。
	/// </summary>
	/// <param name="userId">用户 ID</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// 删除指定用户。
	/// </summary>
	/// <param name="userName">用户名</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task DeleteUserAsync(string userName, CancellationToken cancellationToken = default);

	/// <summary>
	/// 为指定用户创建 API 凭据。
	/// </summary>
	/// <param name="ownerUserId">所属用户 ID</param>
	/// <param name="command">API 凭据创建请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为 API 凭据创建结果</returns>
	Task<CreateApiCredentialCommandResult> CreateApiCredentialAsync(Guid ownerUserId, CreateApiCredentialCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 为指定用户创建 API 凭据。
	/// </summary>
	/// <param name="ownerUserName">所属用户名</param>
	/// <param name="command">API 凭据创建请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为 API 凭据创建结果</returns>
	Task<CreateApiCredentialCommandResult> CreateApiCredentialAsync(string ownerUserName, CreateApiCredentialCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定 API 凭据的显示名称。
	/// </summary>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="command">API 凭据显示名称更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetApiCredentialDisplayNameAsync(Guid apiCredentialId, SetApiCredentialDisplayNameCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定 API 凭据的启用状态。
	/// </summary>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="command">API 凭据启用状态更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetApiCredentialEnabledAsync(Guid apiCredentialId, SetApiCredentialEnabledCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定 API 凭据的角色和设备授权范围。
	/// </summary>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="command">API 凭据角色更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetApiCredentialRoleAsync(Guid apiCredentialId, SetApiCredentialRoleCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定 API 凭据的授权设备范围。
	/// </summary>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="command">API 凭据授权设备更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetApiCredentialAuthorizedDevicesAsync(Guid apiCredentialId, SetApiCredentialAuthorizedDevicesCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 重置指定 API 凭据的操作密钥。
	/// </summary>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为 API 凭据密钥重置结果</returns>
	Task<ResetApiCredentialSecretKeyCommandResult> ResetApiCredentialSecretKeyAsync(Guid apiCredentialId, CancellationToken cancellationToken = default);

	/// <summary>
	/// 删除指定 API 凭据。
	/// </summary>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task DeleteApiCredentialAsync(Guid apiCredentialId, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定设备的授权用户范围。
	/// </summary>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="command">设备授权用户更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetDeviceAuthorizedUsersAsync(Guid deviceId, SetDeviceAuthorizedUsersCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定设备的授权用户范围。
	/// </summary>
	/// <param name="deviceName">设备名称</param>
	/// <param name="command">设备授权用户更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetDeviceAuthorizedUsersAsync(string deviceName, SetDeviceAuthorizedUsersCommand command, CancellationToken cancellationToken = default);
}