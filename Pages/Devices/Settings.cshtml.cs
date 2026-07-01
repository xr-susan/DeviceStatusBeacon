using DeviceStatusBeacon.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Devices;

/// <summary>
/// 设备设置预留页模型
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveDeviceManagement)]
public class SettingsModel(IManagementQueryService queryService) : PageModel {
	/// <summary>
	/// 路由中的设备名称
	/// </summary>
	[BindProperty(SupportsGet = true, Name = "deviceName")]
	public string DeviceName { get; set; } = string.Empty;

	/// <summary>
	/// 设备摘要
	/// </summary>
	public DeviceSummary Device { get; private set; } = null!;

	/// <summary>
	/// 处理设备管理页加载
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) {
		var session = queryService.CreateQuerySessionAsync(User);

		var device = await queryService.GetDeviceByNameAsync(session, DeviceName, cancellationToken);
		if (device is null) {
			return NotFound();
		}

		Device = device;
		return Page();
	}
}