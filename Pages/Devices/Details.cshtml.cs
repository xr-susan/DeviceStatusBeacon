using DeviceStatusBeacon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Devices;

/// <summary>
/// 设备详情页模型
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveUser)]
public class DetailsModel(IManagementQueryService queryService) : PageModel {
	/// <summary>
	/// 页面中展示的最近日志数量
	/// </summary>
	private const int RecentLogCount = 5;

	/// <summary>
	/// 路由中的设备名称
	/// </summary>
	[BindProperty(SupportsGet = true, Name = "deviceName")]
	public string DeviceName { get; set; } = string.Empty;

	/// <summary>
	/// 查询会话
	/// </summary>
	public ManagementSessionData Session { get; private set; } = null!;

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
		var session = queryService.CreateQuerySessionAsync(User);
		Session = session.ToData();

		var device = await queryService.GetDeviceByNameAsync(session, DeviceName, cancellationToken);
		if (device is null) {
			return NotFound();
		}

		Device = device;
		RecentLogs = await queryService.GetLogsByDeviceIdAsync(
			session,
			Device.DeviceId,
			RecentLogCount,
			cancellationToken);

		return Page();
	}
}