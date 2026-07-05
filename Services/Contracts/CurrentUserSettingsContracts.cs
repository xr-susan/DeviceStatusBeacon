using System.ComponentModel.DataAnnotations;

namespace DeviceStatusBeacon.Services.Contracts;

/// <summary>
/// 当前用户显示名称更新请求。
/// </summary>
public sealed class SetCurrentUserDisplayNameCommand : IValidatableObject {
	/// <summary>
	/// 新显示名称。
	/// </summary>
	/// <remarks>设为 null 表示清空显示名称。</remarks>
	public required string? DisplayName { get; init; }

	/// <inheritdoc/>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
		CommandValidation.ValidateOptionalDisplayName(DisplayName, nameof(DisplayName), "当前用户显示名称");
}

/// <summary>
/// 当前用户密码修改请求。
/// </summary>
public sealed class ChangeCurrentUserPasswordCommand {
	/// <summary>
	/// 当前密码。
	/// </summary>
	[Required(ErrorMessage = "当前密码不能为空。")]
	public required string CurrentPassword { get; init; }

	/// <summary>
	/// 新密码。
	/// </summary>
	[Required(ErrorMessage = "新密码不能为空。")]
	public required string NewPassword { get; init; }
}