using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Account;

/// <summary>
/// 当前账号设置页模型。
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveUser)]
public class SettingsModel(
	IAccountSettingsService accountSettingsService,
	IDeviceStatusQueryService deviceStatusQueryService,
	UserManager<User> userManager,
	SignInManager<User> signInManager) : PageModel {
	/// <summary>
	/// 用户显示名称
	/// </summary>
	[BindProperty]
	public string? DisplayName { get; set; }

	/// <summary>
	/// 当前密码
	/// </summary>
	[BindProperty]
	public string? CurrentPassword { get; set; }

	/// <summary>
	/// 新密码
	/// </summary>
	[BindProperty]
	public string? NewPassword { get; set; }

	/// <summary>
	/// 确认新密码
	/// </summary>
	[BindProperty]
	public string? ConfirmPassword { get; set; }

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
	/// 当前账号会话信息
	/// </summary>
	public DeviceStatusSessionData Session { get; private set; } = null!;

	/// <summary>
	/// 处理当前账号设置页加载
	/// </summary>
	public void OnGet() =>
		Session = deviceStatusQueryService.CreateQuerySessionAsync(User).ToData();

	/// <summary>
	/// 处理当前用户显示名称更新
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostSetDisplayNameAsync(CancellationToken cancellationToken) {
		var displayName = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName.Trim();

		try {
			await accountSettingsService.SetDisplayNameAsync(User, new() {
				DisplayName = displayName
			}, cancellationToken);
			await RefreshCurrentSignInAsync();

			StatusMessage = displayName is null ? "显示名称已清空。" : $"显示名称已更新为 {displayName}。";
			return RedirectToPage("/Account/Settings");
		} catch (AccountSettingsException e) {
			return HandleAccountSettingsException(e);
		}
	}

	/// <summary>
	/// 处理当前用户密码修改
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostChangePasswordAsync(CancellationToken cancellationToken) {
		if (string.IsNullOrWhiteSpace(CurrentPassword)) {
			ErrorMessage = "当前密码不能为空。";
			return RedirectToPage("/Account/Settings");
		}

		if (string.IsNullOrWhiteSpace(NewPassword)) {
			ErrorMessage = "新密码不能为空。";
			return RedirectToPage("/Account/Settings");
		}

		if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal)) {
			ErrorMessage = "两次输入的新密码不一致。";
			return RedirectToPage("/Account/Settings");
		}

		try {
			await accountSettingsService.ChangePasswordAsync(User, new() {
				CurrentPassword = CurrentPassword,
				NewPassword = NewPassword
			}, cancellationToken);
			await RefreshCurrentSignInAsync();

			StatusMessage = "密码已修改。";
			return RedirectToPage("/Account/Settings");
		} catch (AccountSettingsException e) {
			return HandleAccountSettingsException(e);
		}
	}

	/// <summary>
	/// 刷新当前登录 Cookie 中的显示名称和安全戳信息
	/// </summary>
	private async Task RefreshCurrentSignInAsync() {
		var (_, principalId, _) = User.GetAuthenticatedPrincipalInfo();
		if (principalId is not Guid userId) {
			return;
		}

		var user = await userManager.FindByIdAsync(userId.ToString());
		if (user is not null) {
			await signInManager.RefreshSignInAsync(user);
		}
	}

	/// <summary>
	/// 处理账号设置业务异常
	/// </summary>
	/// <param name="e">账号设置业务异常</param>
	/// <returns>操作结果</returns>
	private IActionResult HandleAccountSettingsException(AccountSettingsException e) {
		if (e.StatusCode == StatusCodes.Status404NotFound) {
			return NotFound();
		}

		ErrorMessage = e.Message;
		return RedirectToPage("/Account/Settings");
	}
}