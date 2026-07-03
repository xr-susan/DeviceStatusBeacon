using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Account;

/// <summary>
/// 浏览器侧登录页模型
/// </summary>
[AllowAnonymous]
[ResponseCache(NoStore = true)]
public class LoginModel(SignInManager<User> signInManager) : PageModel {
	/// <summary>
	/// 登录表单输入
	/// </summary>
	[BindProperty]
	public InputModel Input { get; set; } = new();

	/// <summary>
	/// 登录成功后尝试回到的本地地址
	/// </summary>
	[BindProperty(SupportsGet = true)]
	public string? ReturnUrl { get; set; }

	/// <summary>
	/// 处理登录页加载
	/// </summary>
	/// <returns>对应的页面结果或重定向结果</returns>
	public IActionResult OnGet() {
		ReturnUrl = NormalizeReturnUrl(ReturnUrl);

		return User.HasInteractiveUserSession()
			? RedirectToResolvedReturnUrl()
			: Page();
	}

	/// <summary>
	/// 处理登录表单提交
	/// </summary>
	/// <returns>对应的页面结果或重定向结果</returns>
	public async Task<IActionResult> OnPostAsync() {
		ReturnUrl = NormalizeReturnUrl(ReturnUrl);

		if (User.HasInteractiveUserSession()) {
			return RedirectToResolvedReturnUrl();
		}

		if (!ModelState.IsValid) {
			return Page();
		}

		var result = await signInManager.PasswordSignInAsync(Input.UserName, Input.Password, isPersistent: false, lockoutOnFailure: true);
		if (result.Succeeded) {
			return RedirectToResolvedReturnUrl();
		}

		ModelState.AddModelError(string.Empty, result.IsLockedOut ? "当前账户已被暂时锁定。" : "用户名或密码不正确。");
		return Page();
	}

	private IActionResult RedirectToResolvedReturnUrl() =>
		!string.IsNullOrWhiteSpace(ReturnUrl)
			? LocalRedirect(ReturnUrl)
			: RedirectToPage("/Dashboard");

	private string? NormalizeReturnUrl(string? returnUrl) {
		if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl)) {
			return null;
		}

		if (!returnUrl.StartsWith('/')) {
			return null;
		}

		var pathBase = Request.PathBase;
		if (!pathBase.HasValue) {
			return returnUrl;
		}

		var pathLength = returnUrl.AsSpan().IndexOfAny('?', '#');
		var path = pathLength >= 0
			? returnUrl[..pathLength]
			: returnUrl;

		return PathString.FromUriComponent(path).StartsWithSegments(pathBase, StringComparison.OrdinalIgnoreCase)
			? returnUrl
			: null;
	}

	/// <summary>
	/// 登录表单输入模型
	/// </summary>
	public sealed class InputModel {
		/// <summary>
		/// 用户名
		/// </summary>
		[Required(ErrorMessage = "请输入用户名。")]
		[Display(Name = "用户名")]
		public string UserName { get; set; } = string.Empty;

		/// <summary>
		/// 密码
		/// </summary>
		[Required(ErrorMessage = "请输入密码。")]
		[DataType(DataType.Password)]
		[Display(Name = "密码")]
		public string Password { get; set; } = string.Empty;
	}
}