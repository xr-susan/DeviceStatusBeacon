using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages;

/// <summary>
/// 日志总览页模型
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveUser)]
public class LogsModel(IDeviceStatusQueryService deviceStatusQueryService) : PageModel {
	/// <summary>
	/// 设备筛选关键字
	/// </summary>
	[BindProperty(SupportsGet = true, Name = "device")]
	public string? DeviceKeyword {
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
	/// 页面数据
	/// </summary>
	public LogListData PageData { get; private set; } = null!;

	/// <summary>
	/// 规范化后的分页数据
	/// </summary>
	public PaginationData Pagination => PageData.Pagination;

	/// <summary>
	/// 每页数量选项
	/// </summary>
	public int[] PageSizeOptions =>
		field ??= PageSizeOptionHelper.Create([20, 50, 100], Pagination.PageSize);

	/// <summary>
	/// 处理日志总览页加载
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task OnGetAsync(CancellationToken cancellationToken) =>
		PageData = await deviceStatusQueryService.GetLogsAsync(
			User,
			DeviceKeyword,
			PageNumber,
			PageSize,
			cancellationToken);
}