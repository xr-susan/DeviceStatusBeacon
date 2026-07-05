using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Devices;

/// <summary>
/// 设备创建页模型。
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveDeviceManagement)]
public class CreateModel(IDeviceManagementService deviceManagementService) : PageModel {
	/// <summary>
	/// 设备名称。
	/// </summary>
	[BindProperty]
	public string? DeviceName { get; set; }

	/// <summary>
	/// 设备显示名称。
	/// </summary>
	[BindProperty]
	public string? DisplayName { get; set; }

	/// <summary>
	/// 成功提示。
	/// </summary>
	[TempData]
	public string? StatusMessage { get; set; }

	/// <summary>
	/// 失败提示。
	/// </summary>
	[TempData]
	public string? ErrorMessage { get; set; }

	/// <summary>
	/// 本次创建后生成的新设备操作密钥。
	/// </summary>
	[TempData]
	public string? ResetSecretKey { get; set; }

	/// <summary>
	/// 处理设备创建。
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken) {
		if (string.IsNullOrWhiteSpace(DeviceName)) {
			ErrorMessage = "设备名称不能为空。";
			return RedirectToPage("/Devices/Create");
		}

		try {
			var result = await deviceManagementService.CreateAsync(new() {
				DeviceName = DeviceName.Trim(),
				DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName.Trim()
			}, cancellationToken);

			ResetSecretKey = result.SecretKey;
			StatusMessage = $"设备 {result.DeviceName} 已创建。";
			return RedirectToPage("/Devices/Settings", new {
				deviceName = result.DeviceName
			});
		} catch (DeviceManagementCommandException e) {
			ErrorMessage = e.Message;
			return RedirectToPage("/Devices/Create");
		}
	}
}