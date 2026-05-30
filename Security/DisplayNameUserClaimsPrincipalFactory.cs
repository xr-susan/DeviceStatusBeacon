using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DeviceStatusBeacon.Security;

/// <summary>
/// 为 Identity 登录主体补充显示名称声明，避免后续请求重复查库
/// </summary>
public sealed class DisplayNameUserClaimsPrincipalFactory(
	UserManager<User> userManager,
	RoleManager<IdentityRole<Guid>> roleManager,
	IOptions<IdentityOptions> optionsAccessor)
	: UserClaimsPrincipalFactory<User, IdentityRole<Guid>>(userManager, roleManager, optionsAccessor) {
	/// <inheritdoc/>
	protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user) {
		var identity = await base.GenerateClaimsAsync(user);

		// 如果用户具有显示名称，则将其添加到当前登录主体的声明中，供后续查询会话使用
		if (!string.IsNullOrWhiteSpace(user.DisplayName)) {
			// 先移除原有的显示名称声明，提前转为数组避免修改集合时枚举器失效
			foreach (var claim in identity.FindAll(ClaimTypes.GivenName).ToArray()) {
				identity.RemoveClaim(claim);
			}

			// 再添加新的显示名称声明
			identity.AddClaim(new(ClaimTypes.GivenName, user.DisplayName));
		}

		return identity;
	}
}