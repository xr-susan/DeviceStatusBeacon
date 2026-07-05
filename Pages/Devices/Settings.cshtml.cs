using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Devices;

/// <summary>
/// 设备管理页模型
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveDeviceManagement)]
public class SettingsModel(
	IManagementQueryService queryService,
	IDeviceManagementService deviceManagementService) : PageModel {
	/// <summary>
	/// 路由中的设备名称
	/// </summary>
	[BindProperty(SupportsGet = true, Name = "deviceName")]
	public string DeviceName { get; set; } = string.Empty;

	/// <summary>
	/// 新设备名称
	/// </summary>
	[BindProperty]
	public string? NewDeviceName { get; set; }

	/// <summary>
	/// 设备显示名称
	/// </summary>
	[BindProperty]
	public string? DisplayName { get; set; }

	/// <summary>
	/// 设备启用状态
	/// </summary>
	[BindProperty]
	public bool Enabled { get; set; }

	/// <summary>
	/// 删除确认设备名称
	/// </summary>
	[BindProperty]
	public string? DeleteConfirmationDeviceName { get; set; }

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
	/// 本次重置后生成的新设备操作密钥
	/// </summary>
	[TempData]
	public string? ResetSecretKey { get; set; }

	/// <summary>
	/// 设备摘要
	/// </summary>
	public DeviceSummary Device { get; private set; } = null!;

	/// <summary>
	/// 当前设备是否通过页面删除预检
	/// </summary>
	/// <remarks>此处是非严格的预检，不进行任何高成本查询，实际删除操作会在服务层和 SQL 语句中进行严格检查</remarks>
	public bool CanBeDelete => Device is {
		Enabled: false
	} && (Device.LatestLogTime is null
		|| Device.LatestLogTime < DateTime.UtcNow.Subtract(IDeviceManagementService.DeleteRecentLogBlockWindow));

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

		if (!string.Equals(DeviceName, device.DeviceName, StringComparison.Ordinal)) {
			// 设备名称大小写不匹配，重定向到正确的设备名称
			return RedirectToSettingsPage(device.DeviceName);
		}

		Device = device;
		Enabled = device.Enabled;
		return Page();
	}

	/// <summary>
	/// 处理设备重命名
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostRenameAsync(CancellationToken cancellationToken) {
		if (string.IsNullOrWhiteSpace(NewDeviceName)) {
			ErrorMessage = "新设备名称不能为空。";
			return RedirectToSettingsPage();
		}

		var newDeviceName = NewDeviceName.Trim();

		try {
			await deviceManagementService.RenameAsync(DeviceName, new() {
				NewDeviceName = newDeviceName
			}, cancellationToken);

			StatusMessage = $"设备已重命名为 {newDeviceName}。";
			return RedirectToSettingsPage(newDeviceName);
		} catch (DeviceManagementCommandException e) {
			return HandleDeviceManagementCommandException(e);
		}
	}

	/// <summary>
	/// 处理设备显示名称更新
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostSetDisplayNameAsync(CancellationToken cancellationToken) {
		var displayName = string.IsNullOrEmpty(DisplayName)
			? null
			: DisplayName;

		try {
			await deviceManagementService.SetDisplayNameAsync(DeviceName, new() {
				DisplayName = displayName
			}, cancellationToken);

			StatusMessage = displayName is null ? "设备显示名称已清空。" : $"设备显示名称已更新为 {displayName}。";
			return RedirectToSettingsPage();
		} catch (DeviceManagementCommandException e) {
			return HandleDeviceManagementCommandException(e);
		}
	}

	/// <summary>
	/// 处理设备启用状态更新
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostSetEnabledAsync(CancellationToken cancellationToken) {
		try {
			await deviceManagementService.SetEnabledAsync(DeviceName, new() {
				Enabled = Enabled
			}, cancellationToken);

			StatusMessage = Enabled ? "设备已启用。" : "设备已停用。";
			return RedirectToSettingsPage();
		} catch (DeviceManagementCommandException e) {
			return HandleDeviceManagementCommandException(e);
		}
	}

	/// <summary>
	/// 处理设备操作密钥重置
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostResetSecretKeyAsync(CancellationToken cancellationToken) {
		try {
			var commandResult = await deviceManagementService.ResetSecretKeyAsync(DeviceName, cancellationToken);
			ResetSecretKey = commandResult.SecretKey;
			StatusMessage = "设备操作密钥已重置。";
			return RedirectToSettingsPage();
		} catch (DeviceManagementCommandException e) {
			return HandleDeviceManagementCommandException(e);
		}
	}

	/// <summary>
	/// 处理设备删除
	/// </summary>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken) {
		var session = queryService.CreateQuerySessionAsync(User);

		var device = await queryService.GetDeviceByNameAsync(session, DeviceName, cancellationToken);
		if (device is null) {
			return NotFound();
		}

		if (!string.Equals(DeleteConfirmationDeviceName?.Trim(), device.DeviceName, StringComparison.Ordinal)) {
			ErrorMessage = "删除确认设备名称不匹配。";
			return RedirectToSettingsPage(device.DeviceName);
		}

		try {
			await deviceManagementService.DeleteAsync(device.DeviceName, cancellationToken);

			StatusMessage = $"设备 {device.DeviceName} 已删除。";
			return RedirectToPage("/Devices/Index");
		} catch (DeviceManagementCommandException e) {
			return HandleDeviceManagementCommandException(e, device.DeviceName);
		}
	}

	/// <summary>
	/// 处理设备管理命令异常
	/// </summary>
	/// <param name="e">设备管理命令异常</param>
	/// <param name="deviceName">获取到的设备名称，默认为 null</param>
	/// <returns>操作结果</returns>
	private IActionResult HandleDeviceManagementCommandException(DeviceManagementCommandException e, string? deviceName = null) {
		if (e.StatusCode == StatusCodes.Status404NotFound) {
			return NotFound();
		}
		ErrorMessage = e.Message;
		return RedirectToSettingsPage(deviceName);
	}

	/// <summary>
	/// 重定向回当前设备管理页。
	/// </summary>
	/// <param name="deviceName">目标设备名称</param>
	/// <returns>设备管理页重定向结果</returns>
	private RedirectToPageResult RedirectToSettingsPage(string? deviceName = null) =>
		RedirectToPage("/Devices/Settings", new {
			deviceName = deviceName ?? DeviceName
		});
}