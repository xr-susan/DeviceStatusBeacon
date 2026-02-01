using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DeviceStatusBeacon.Security;

/// <inheritdoc/>
public class AuthorizationHandlerV1(DeviceStatusBeaconContext dbContext) : AuthorizationHandler<IAuthorizationRequirement, Guid> {
	/// <inheritdoc/>
	protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IAuthorizationRequirement requirement, Guid deviceId) {
		// 从用户声明中提取角色和标识符
		var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
		var nameIdentifier = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

		// 缺少必要的声明，拒绝访问
		if (string.IsNullOrWhiteSpace(role)
			|| !Guid.TryParse(nameIdentifier, out var requesterId)) {
			return;
		}

		// 管理员和完全查询角色有权访问所有设备
		if (role is nameof(AccountRole.Administrator) or nameof(AccountRole.FullQuery)) {
			context.Succeed(requirement);
			return;
		}

		// 设备角色只能访问其自身
		if (role == "Device") {
			if (requesterId == deviceId) {
				context.Succeed(requirement);
			}
			return;
		}

		// 限制查询角色只能访问其拥有的设备
		if (role == nameof(AccountRole.LimitedQuery)) {
			var hasPermission = await dbContext.Devices
				.AnyAsync(d => d.DeviceId == deviceId
					&& d.AuthorizedAccounts.Any(a => a.AccountId == requesterId));

			if (hasPermission) {
				context.Succeed(requirement);
				return;
			}
		}
	}
}