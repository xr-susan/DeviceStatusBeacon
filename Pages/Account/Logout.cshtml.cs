using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Account;

/// <summary>
/// 浏览器侧登出页模型
/// </summary>
[AllowAnonymous]
[ResponseCache(NoStore = true)]
public class LogoutModel(SignInManager<User> signInManager) : PageModel {
	/// <summary>
	/// 处理登出页直接访问
	/// </summary>
	/// <returns>重定向到首页的结果</returns>
	public IActionResult OnGet() => RedirectToPage("/Index");

	/// <summary>
	/// 处理登出提交
	/// </summary>
	/// <returns>重定向到首页的结果</returns>
	public async Task<IActionResult> OnPostAsync() {
		if (User.HasInteractiveUserSession()) {
			await signInManager.SignOutAsync();
		}

		return RedirectToPage("/Index");
	}
}