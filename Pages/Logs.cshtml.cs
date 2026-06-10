using DeviceStatusBeacon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages;

/// <summary>
/// 日志总览页模型
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveUser)]
public class LogsModel(IManagementQueryService queryService) : PageModel {
	/// <summary>
	/// 设备筛选关键字
	/// </summary>
	[BindProperty(SupportsGet = true, Name = "device")]
	public string? DeviceKeyword { get; set; }

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
	public LogListData PageData { get; private set; } = null!;

	/// <summary>
	/// 处理日志总览页加载
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task OnGetAsync(CancellationToken cancellationToken) {
		DeviceKeyword = string.IsNullOrWhiteSpace(DeviceKeyword)
			? null
			: DeviceKeyword.Trim();

		PageData = await queryService.GetLogsAsync(
			User,
			DeviceKeyword,
			PageNumber,
			PageSize,
			cancellationToken);

		PageNumber = PageData.Pagination.PageNumber;
		PageSize = PageData.Pagination.PageSize;
	}
}