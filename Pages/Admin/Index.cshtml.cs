using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Admin;

/// <summary>
/// 管理后台入口页模型。
/// </summary>
[Authorize(Policy = AuthorizationPolicyNames.InteractiveAdminOnly)]
public class IndexModel : PageModel;