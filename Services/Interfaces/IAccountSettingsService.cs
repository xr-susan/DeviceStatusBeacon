using System.Security.Claims;

namespace DeviceStatusBeacon.Services.Interfaces;

/// <summary>
/// 账号设置服务。
/// </summary>
public interface IAccountSettingsService {
	/// <summary>
	/// 更新当前用户的显示名称。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="command">显示名称更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetDisplayNameAsync(ClaimsPrincipal principal, SetCurrentUserDisplayNameCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 修改当前用户的密码。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="command">密码修改请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task ChangePasswordAsync(ClaimsPrincipal principal, ChangeCurrentUserPasswordCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 为当前用户创建 API 凭据。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="command">API 凭据创建请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为 API 凭据创建结果</returns>
	Task<CreateApiCredentialCommandResult> CreateApiCredentialAsync(ClaimsPrincipal principal, CreateApiCredentialCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新当前用户指定 API 凭据的显示名称。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="command">API 凭据显示名称更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetApiCredentialDisplayNameAsync(ClaimsPrincipal principal, Guid apiCredentialId, SetApiCredentialDisplayNameCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新当前用户指定 API 凭据的启用状态。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="command">API 凭据启用状态更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetApiCredentialEnabledAsync(ClaimsPrincipal principal, Guid apiCredentialId, SetApiCredentialEnabledCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新当前用户指定 API 凭据的角色和设备授权范围。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="command">API 凭据角色更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetApiCredentialRoleAsync(ClaimsPrincipal principal, Guid apiCredentialId, SetApiCredentialRoleCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新当前用户指定 API 凭据的授权设备范围。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="command">API 凭据授权设备更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetApiCredentialAuthorizedDevicesAsync(ClaimsPrincipal principal, Guid apiCredentialId, SetApiCredentialAuthorizedDevicesCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 重置当前用户指定 API 凭据的操作密钥。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为 API 凭据密钥重置结果</returns>
	Task<ResetApiCredentialSecretKeyCommandResult> ResetApiCredentialSecretKeyAsync(ClaimsPrincipal principal, Guid apiCredentialId, CancellationToken cancellationToken = default);

	/// <summary>
	/// 删除当前用户指定 API 凭据。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task DeleteApiCredentialAsync(ClaimsPrincipal principal, Guid apiCredentialId, CancellationToken cancellationToken = default);
}