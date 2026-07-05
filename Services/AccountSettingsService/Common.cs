using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace DeviceStatusBeacon.Services;

/// <summary>
/// 账号设置服务。
/// </summary>
public sealed partial class AccountSettingsService(
	DeviceStatusBeaconContext dbContext,
	UserManager<User> userManager,
	IAccessAdministrationService accessAdministrationService) : IAccountSettingsService {
	/// <summary>
	/// 当前用户摘要。
	/// </summary>
	/// <param name="UserId">用户 ID</param>
	/// <param name="Role">用户角色</param>
	private sealed record CurrentUserTarget(
		Guid UserId,
		PrincipalRole Role
	);

	/// <summary>
	/// 从当前登录主体中读取用户 ID，并查询当前用户角色。
	/// </summary>
	/// <remarks>
	/// 此方法不信任 Cookie 中的角色声明作为最终依据，每次写入前从数据库读取当前角色。
	/// 当 <paramref name="ownedApiCredentialId"/> 不为 null 时，还会确认该 API 凭据归属于当前用户。
	/// </remarks>
	/// <param name="principal">当前登录主体</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <param name="ownedApiCredentialId">需要同时确认归属的 API 凭据 ID；为 null 时仅查询当前用户摘要</param>
	/// <returns>当前用户摘要</returns>
	private async Task<CurrentUserTarget> GetCurrentUserTargetAsync(ClaimsPrincipal principal, CancellationToken cancellationToken, Guid? ownedApiCredentialId = null) {
		var (principalKind, principalId, _) = principal.GetAuthenticatedPrincipalInfo();
		if (principalKind != PrincipalKind.User || principalId is not Guid userId) {
			throw new AccountSettingsException(StatusCodes.Status401Unauthorized, "当前用户未登录");
		}

		// 不信任 Cookie 中的角色声明作为最终依据，每次写入前从数据库读取当前角色
		// 避免 Cookie 角色五分钟的有效期内，用户角色被管理员修改后仍然可以越权操作
		var target = await dbContext.Users
			.AsNoTracking()
			.Where(user => user.Id == userId)
			.Select(user => new {
				user.Id,
				RoleName = user.UserRoles.Select(userRole => userRole.Role.Name).SingleOrDefault(),
				OwnsApiCredential = ownedApiCredentialId == null
					|| user.ApiCredentials.Any(credential => credential.ApiCredentialId == ownedApiCredentialId)
			})
			.SingleOrDefaultAsync(cancellationToken)
			?? throw new AccountSettingsException(StatusCodes.Status404NotFound, "未找到当前用户");

		if (!target.OwnsApiCredential) {
			// 当前用户只能操作自己归属的 API 凭据
			throw new AccountSettingsException(StatusCodes.Status404NotFound, "未找到指定的 API 凭据");
		}

		return PrincipalRole.TryParse(target.RoleName, out var role)
			? new(target.Id, role)
			: throw new AccountSettingsException(StatusCodes.Status409Conflict, "当前用户未正确设置角色");
	}

	/// <summary>
	/// 将访问管理服务异常转换为账号设置服务异常。
	/// </summary>
	/// <param name="exception">访问管理服务异常</param>
	/// <returns>账号设置服务异常</returns>
	private static AccountSettingsException ToAccountSettingsException(AccessAdministrationException exception) =>
		new(exception.StatusCode, exception.Message);
}