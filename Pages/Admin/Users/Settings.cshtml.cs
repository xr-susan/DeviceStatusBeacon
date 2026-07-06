using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Admin.Users;

/// <summary>
/// 管理员单用户设置页模型
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveAdminOnly)]
public class SettingsModel(
	IAccessAdministrationQueryService accessAdministrationQueryService,
	IAccessAdministrationService accessAdministrationService) : PageModel {
	/// <summary>
	/// 路由中的用户名
	/// </summary>
	[BindProperty(SupportsGet = true, Name = "userName")]
	public string UserName { get; set; } = string.Empty;

	/// <summary>
	/// 新用户名
	/// </summary>
	[BindProperty]
	public string? NewUserName { get; set; }

	/// <summary>
	/// 用户显示名称
	/// </summary>
	[BindProperty]
	public string? DisplayName { get; set; }

	/// <summary>
	/// 用户新密码
	/// </summary>
	[BindProperty]
	public string? NewPassword { get; set; }

	/// <summary>
	/// 用户角色
	/// </summary>
	[BindProperty]
	public PrincipalRole Role { get; set; }

	/// <summary>
	/// 删除确认用户名
	/// </summary>
	[BindProperty]
	public string? DeleteConfirmationUserName { get; set; }

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
	/// 当前用户 ID
	/// </summary>
	public Guid? CurrentUserId => field ??= User.GetAuthenticatedPrincipalInfo().PrincipalId;

	/// <summary>
	/// 目标用户
	/// </summary>
	public AccessUserSummary UserSummary { get; private set; } = null!;

	/// <summary>
	/// 处理单用户设置页加载
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为页面加载结果</returns>
	public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) {
		var user = await accessAdministrationQueryService.GetUserByNameAsync(UserName, cancellationToken);
		if (user is null) {
			return NotFound();
		}

		if (!string.Equals(UserName, user.UserName, StringComparison.Ordinal)) {
			// 用户名大小写不匹配，重定向到当前存储的用户名，保持地址栏与规范名称一致
			return RedirectToSettingsPage(user.UserName);
		}

		UserSummary = user;
		return Page();
	}

	/// <summary>
	/// 处理用户重命名
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostRenameAsync(CancellationToken cancellationToken) {
		if (string.IsNullOrWhiteSpace(NewUserName)) {
			ErrorMessage = "新用户名不能为空。";
			return RedirectToSettingsPage();
		}

		var newUserName = NewUserName.Trim();
		try {
			await accessAdministrationService.RenameUserAsync(UserName, new() {
				NewUserName = newUserName
			}, cancellationToken);
			StatusMessage = $"用户已重命名为 {newUserName}。";
			return RedirectToSettingsPage(newUserName);
		} catch (AccessAdministrationException e) {
			return HandleAccessAdministrationException(e);
		}
	}

	/// <summary>
	/// 处理用户显示名称更新
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostSetDisplayNameAsync(CancellationToken cancellationToken) {
		var displayName = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName.Trim();

		try {
			await accessAdministrationService.SetUserDisplayNameAsync(UserName, new() {
				DisplayName = displayName
			}, cancellationToken);
			StatusMessage = displayName is null ? "用户显示名称已清空。" : $"用户显示名称已更新为 {displayName}。";
			return RedirectToSettingsPage();
		} catch (AccessAdministrationException e) {
			return HandleAccessAdministrationException(e);
		}
	}

	/// <summary>
	/// 处理用户密码重置
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostResetPasswordAsync(CancellationToken cancellationToken) {
		if (string.IsNullOrWhiteSpace(NewPassword)) {
			ErrorMessage = "新密码不能为空。";
			return RedirectToSettingsPage();
		}

		try {
			await accessAdministrationService.ResetUserPasswordAsync(UserName, new() {
				NewPassword = NewPassword
			}, cancellationToken);
			StatusMessage = "用户密码已重置。";
			return RedirectToSettingsPage();
		} catch (AccessAdministrationException e) {
			return HandleAccessAdministrationException(e);
		}
	}

	/// <summary>
	/// 处理用户角色更新
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostSetRoleAsync(CancellationToken cancellationToken) {
		var target = await accessAdministrationQueryService.GetUserByNameAsync(UserName, cancellationToken);
		if (target is null) {
			return NotFound();
		}

		if (target.UserId == CurrentUserId && Role != PrincipalRole.Administrator) {
			ErrorMessage = "不能在当前管理会话中降低自己的管理员角色。";
			return RedirectToSettingsPage(target.UserName);
		}

		try {
			await accessAdministrationService.SetUserRoleAsync(target.UserName, new() {
				Role = Role
			}, cancellationToken);
			StatusMessage = $"用户角色已更新为 {Role}。";
			return RedirectToSettingsPage(target.UserName);
		} catch (AccessAdministrationException e) {
			return HandleAccessAdministrationException(e, target.UserName);
		}
	}

	/// <summary>
	/// 处理用户删除
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken) {
		var target = await accessAdministrationQueryService.GetUserByNameAsync(UserName, cancellationToken);
		if (target is null) {
			return NotFound();
		}

		if (target.UserId == CurrentUserId) {
			ErrorMessage = "不能在当前管理会话中删除自己。";
			return RedirectToSettingsPage(target.UserName);
		}

		if (!string.Equals(DeleteConfirmationUserName?.Trim(), target.UserName, StringComparison.Ordinal)) {
			ErrorMessage = "删除确认用户名不匹配。";
			return RedirectToSettingsPage(target.UserName);
		}

		try {
			await accessAdministrationService.DeleteUserAsync(target.UserName, cancellationToken);
			StatusMessage = "用户已删除。";
			return RedirectToPage("/Admin/Users/Index");
		} catch (AccessAdministrationException e) {
			return HandleAccessAdministrationException(e, target.UserName);
		}
	}

	/// <summary>
	/// 处理访问管理业务异常
	/// </summary>
	/// <param name="e">访问管理业务异常</param>
	/// <param name="userName">获取到的用户名，默认为 null</param>
	/// <returns>操作结果</returns>
	private IActionResult HandleAccessAdministrationException(AccessAdministrationException e, string? userName = null) {
		if (e.StatusCode == StatusCodes.Status404NotFound) {
			return NotFound();
		}
		ErrorMessage = e.Message;
		return RedirectToSettingsPage(userName);
	}

	/// <summary>
	/// 重定向回单用户设置页
	/// </summary>
	/// <param name="userName">目标用户名</param>
	/// <returns>单用户设置页重定向结果</returns>
	private RedirectToPageResult RedirectToSettingsPage(string? userName = null) =>
		RedirectToPage("/Admin/Users/Settings", new {
			userName = userName ?? UserName
		});
}