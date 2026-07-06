using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages;

/// <summary>
/// Agent 页面模型。
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveDeviceManagement)]
public class AgentModel : PageModel {
	/// <summary>
	/// 处理 Agent 页面加载。
	/// </summary>
	public void OnGet() {
	}
}