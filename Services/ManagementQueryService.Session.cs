using System.Security.Claims;

namespace DeviceStatusBeacon.Services;

public sealed partial class ManagementQueryService {
	/// <inheritdoc/>
	public ManagementQuerySession CreateQuerySessionAsync(ClaimsPrincipal principal) {
		var userId = TryReadUserId(principal);
		var userName = principal.Identity?.Name ?? string.Empty;
		var displayName = principal.FindFirstValue(ClaimTypes.GivenName);
		var role = TryReadPrincipalRole(principal);

		return new(userId, userName, displayName, role);
	}

	/// <inheritdoc/>
	public ManagementQuerySession CreatePrivilegedQuerySession(string userName = "CLI") =>
		new(
			UserId: null,
			UserName: userName,
			DisplayName: null,
			Role: PrincipalRole.Administrator);

	/// <summary>
	/// 尝试从当前登录主体中读取用户 ID。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <returns>用户 ID；如果当前主体不包含合法的用户 ID，则返回 null</returns>
	private static Guid? TryReadUserId(ClaimsPrincipal principal) {
		var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
		return Guid.TryParse(rawUserId, out var userId) ? userId : null;
	}

	/// <summary>
	/// 尝试从当前登录主体中读取角色。
	/// </summary>
	/// <remarks>
	/// 当前数据模型约束单个主体最多只绑定一个角色，如果存在多个角色声明，抛出异常以提前暴露数据问题。
	/// </remarks>
	/// <param name="principal">当前登录主体</param>
	/// <returns>角色；如果当前主体不包含合法的管理角色，默认为 <see cref="PrincipalRole.LimitedQuery"/></returns>
	private static PrincipalRole TryReadPrincipalRole(ClaimsPrincipal principal) {
		PrincipalRole? role = null;

		var roleClaims = principal.FindAll(ClaimTypes.Role);
		foreach (var claim in roleClaims) {
			var parsedRole = TryParsePrincipalRole(claim.Value);
			if (parsedRole is null) {
				continue;
			}

			if (role is not null) {
				throw new InvalidOperationException($"当前登录主体包含多个管理角色声明，无法确定最终角色：{string.Join(", ", roleClaims.Select(claim => claim.Value))}");
			}
			role = parsedRole;
		}

		return role ?? PrincipalRole.LimitedQuery;
	}

	/// <summary>
	/// 尝试把角色名称解析为 <see cref="PrincipalRole"/>。
	/// </summary>
	/// <param name="value">角色名称</param>
	/// <returns>解析得到的角色；如果角色名称无效，则返回 null</returns>
	private static PrincipalRole? TryParsePrincipalRole(string? value) =>
		Enum.TryParse<PrincipalRole>(value, true, out var role) ? role : null;
}