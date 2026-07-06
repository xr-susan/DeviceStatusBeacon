using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Admin.Users;

/// <summary>
/// 管理员用户列表页模型
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveAdminOnly)]
public class IndexModel(IAccessAdministrationQueryService accessAdministrationQueryService) : PageModel {
	/// <summary>
	/// 搜索关键字
	/// </summary>
	[BindProperty(SupportsGet = true, Name = "q")]
	public string? SearchTerm { get; set; }

	/// <summary>
	/// 页码
	/// </summary>
	[BindProperty(SupportsGet = true, Name = "page")]
	public int PageNumber { get; set; } = 1;

	/// <summary>
	/// 每页数量
	/// </summary>
	[BindProperty(SupportsGet = true, Name = "pageSize")]
	public int PageSize { get; set; } = 20;

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
	/// 页面数据
	/// </summary>
	public UserListData PageData { get; private set; } = null!;

	/// <summary>
	/// 处理用户列表页加载
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task OnGetAsync(CancellationToken cancellationToken) {
		SearchTerm = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm.Trim();

		PageData = await accessAdministrationQueryService.GetUsersAsync(
			SearchTerm,
			PageNumber,
			PageSize,
			cancellationToken);

		PageNumber = PageData.Pagination.PageNumber;
		PageSize = PageData.Pagination.PageSize;
	}
}