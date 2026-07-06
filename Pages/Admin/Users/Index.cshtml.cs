using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DeviceStatusBeacon.Pages;

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
	public string? SearchTerm {
		get;
		set => field = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	/// <summary>
	/// 页码
	/// </summary>
	[BindProperty(SupportsGet = true, Name = "pageNumber")]
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
	/// 规范化后的分页数据
	/// </summary>
	public PaginationData Pagination => PageData.Pagination;

	/// <summary>
	/// 每页数量选项
	/// </summary>
	public int[] PageSizeOptions =>
		field ??= PageSizeOptionHelper.Create([10, 20, 50], Pagination.PageSize);

	/// <summary>
	/// 处理用户列表页加载
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task OnGetAsync(CancellationToken cancellationToken) =>
		PageData = await accessAdministrationQueryService.GetUsersAsync(
			SearchTerm,
			PageNumber,
			PageSize,
			cancellationToken);
}