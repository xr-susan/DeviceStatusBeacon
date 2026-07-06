using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Admin.Users;

/// <summary>
/// 管理员用户创建页模型
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveAdminOnly)]
public class CreateModel(IAccessAdministrationService accessAdministrationService) : PageModel {
	/// <summary>
	/// 用户名
	/// </summary>
	[BindProperty]
	public string? UserName { get; set; }

	/// <summary>
	/// 用户显示名称
	/// </summary>
	[BindProperty]
	public string? DisplayName { get; set; }

	/// <summary>
	/// 用户初始密码
	/// </summary>
	[BindProperty]
	public string? Password { get; set; }

	/// <summary>
	/// 用户角色
	/// </summary>
	[BindProperty]
	public PrincipalRole Role { get; set; } = PrincipalRole.LimitedQuery;

	/// <summary>
	/// 成功提示
	/// </summary>
	[TempData]
	public string? StatusMessage { get; set; }

	/// <summary>
	/// 失败提示
	/// </summary>
	[TempData]
	public string? ErrorMessage { get; set; }

	/// <summary>
	/// 处理用户创建
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken) {
		if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password)) {
			ErrorMessage = "用户名和初始密码不能为空。";
			return RedirectToPage("/Admin/Users/Create");
		}

		try {
			var result = await accessAdministrationService.CreateUserAsync(new() {
				UserName = UserName.Trim(),
				DisplayName = string.IsNullOrEmpty(DisplayName) ? null : DisplayName,
				Password = Password,
				Role = Role
			}, cancellationToken);

			StatusMessage = $"用户 {result.UserName} 已创建。";
			return RedirectToPage("/Admin/Users/Settings", new {
				userName = result.UserName
			});
		} catch (AccessAdministrationException e) {
			ErrorMessage = e.Message;
			return RedirectToPage("/Admin/Users/Create");
		}
	}
}