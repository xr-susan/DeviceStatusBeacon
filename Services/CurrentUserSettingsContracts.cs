using System.ComponentModel.DataAnnotations;

namespace DeviceStatusBeacon.Services;

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

/// <summary>
/// 表示当前用户设置命令执行过程中出现的业务失败。
/// </summary>
public sealed class CurrentUserSettingsCommandException(int statusCode, string message)
	: ServiceCommandException(statusCode, GetProblemTitle(statusCode), message) {
	/// <summary>
	/// 获取当前用户设置失败对应的错误标题。
	/// </summary>
	/// <param name="statusCode">HTTP 状态码</param>
	/// <returns>用于 ProblemDetails 的错误标题</returns>
	private static string GetProblemTitle(int statusCode) => statusCode switch {
		StatusCodes.Status400BadRequest => "当前用户设置请求无效",
		StatusCodes.Status401Unauthorized => "当前用户未登录",
		StatusCodes.Status403Forbidden => "当前用户无权执行该操作",
		StatusCodes.Status404NotFound => "目标资源不存在",
		StatusCodes.Status409Conflict => "当前用户设置状态冲突",
		StatusCodes.Status422UnprocessableEntity => "当前用户设置请求语义无效",
		_ => "当前用户设置失败"
	};
}