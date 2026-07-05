using System.ComponentModel.DataAnnotations;

namespace DeviceStatusBeacon.Services.Contracts;

/// <summary>
/// 当前用户显示名称更新请求。
/// </summary>
public sealed class SetCurrentUserDisplayNameCommand {
	/// <summary>
	/// 新显示名称。
	/// </summary>
	/// <remarks>设为 null 表示清空显示名称。</remarks>
	public required string? DisplayName { get; init; }
}

/// <summary>
/// 当前用户密码修改请求。
/// </summary>
public sealed class ChangeCurrentUserPasswordCommand {
	/// <summary>
	/// 当前密码。
	/// </summary>
	[Required]
	public required string CurrentPassword { get; init; }

	/// <summary>
	/// 新密码。
	/// </summary>
	[Required]
	public required string NewPassword { get; init; }
}