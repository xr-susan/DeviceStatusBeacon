using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages;

/// <summary>
/// 公开首页模型
/// </summary>
[AllowAnonymous]
public class IndexModel : PageModel {
	/// <summary>
	/// 处理首页加载
	/// </summary>
	/// <returns>对应的页面结果或重定向结果</returns>
	public IActionResult OnGet() =>
		User.HasInteractiveUserSession()
			? RedirectToPage("/Dashboard")
			: Page();
}