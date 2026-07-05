using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Devices;

/// <summary>
/// 设备详情页模型
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveUser)]
public class DetailsModel(IDeviceStatusQueryService deviceStatusQueryService) : PageModel {
	/// <summary>
	/// 路由中的设备名称
	/// </summary>
	[BindProperty(SupportsGet = true, Name = "deviceName")]
	public string DeviceName { get; set; } = string.Empty;

	/// <summary>
	/// 查询会话
	/// </summary>
	public DeviceStatusSessionData Session { get; private set; } = null!;

	/// <summary>
	/// 设备摘要
	/// </summary>
	public DeviceSummary Device { get; private set; } = null!;

	/// <summary>
	/// 最近日志
	/// </summary>
	public IReadOnlyCollection<OnlineLogSummary> RecentLogs { get; private set; } = [];

	/// <summary>
	/// 处理设备详情页加载
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) {
		var session = deviceStatusQueryService.CreateQuerySessionAsync(User);
		Session = session.ToData();

		var deviceDetails = await deviceStatusQueryService.GetDeviceDetailsByNameAsync(
			session,
			DeviceName,
			cancellationToken);
		if (deviceDetails is null) {
			return NotFound();
		}

		Device = deviceDetails.Device;
		RecentLogs = deviceDetails.RecentLogs;
		return Page();
	}
}