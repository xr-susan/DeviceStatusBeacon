using System.Security.Claims;

namespace DeviceStatusBeacon.Services;

public sealed partial class ManagementQueryService {
	/// <inheritdoc/>
	public ManagementQuerySession CreateQuerySessionAsync(ClaimsPrincipal principal) {
		var userId = principal.TryReadUserId();
		var userName = principal.Identity?.Name ?? string.Empty;
		var displayName = principal.FindFirstValue(ClaimTypes.GivenName);
		var role = principal.TryReadPrincipalRole();

		return new(userId, userName, displayName, role);
	}

	/// <inheritdoc/>
	public ManagementQuerySession CreatePrivilegedQuerySession(string userName = "CLI") =>
		new(
			UserId: null,
			UserName: userName,
			DisplayName: null,
			Role: PrincipalRole.Administrator);
}