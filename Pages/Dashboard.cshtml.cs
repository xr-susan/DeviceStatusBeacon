using DeviceStatusBeacon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages;

/// <summary>
/// Dashboard 页面模型
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveUser)]
public class DashboardModel(IManagementQueryService queryService) : PageModel {
	/// <summary>
	/// Dashboard 首屏摘要数据
	/// </summary>
	public DashboardOverviewData Overview { get; private set; } = null!;

	/// <summary>
	/// Dashboard 按需加载数据路径
	/// </summary>
	public string ActivityApiPath { get; private set; } = "/api/dashboard/activity";

	/// <summary>
	/// 处理 Dashboard 页面加载
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task OnGetAsync(CancellationToken cancellationToken) {
		Overview = await queryService.GetDashboardOverviewAsync(User, cancellationToken);
		ActivityApiPath = Url.Content("~/api/dashboard/activity");
	}
}