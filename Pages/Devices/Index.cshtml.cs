using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Devices;

/// <summary>
/// 设备列表页模型
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveUser)]
public class IndexModel(IManagementQueryService queryService) : PageModel {
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
	/// 页面数据
	/// </summary>
	public DeviceListData PageData { get; private set; } = null!;

	/// <summary>
	/// 处理设备列表页加载
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task OnGetAsync(CancellationToken cancellationToken) {
		SearchTerm = string.IsNullOrWhiteSpace(SearchTerm)
			? null
			: SearchTerm.Trim();

		PageData = await queryService.GetDevicesAsync(
			User,
			SearchTerm,
			PageNumber,
			PageSize,
			cancellationToken);

		PageNumber = PageData.Pagination.PageNumber;
		PageSize = PageData.Pagination.PageSize;
	}
}