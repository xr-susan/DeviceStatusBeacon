namespace DeviceStatusBeacon.Services;

/// <summary>
/// 表示服务层命令执行过程中出现的业务失败。
/// </summary>
public abstract class ServiceCommandException(int statusCode, string problemTitle, string message) : Exception(message) {
	/// <summary>
	/// 对应的 HTTP 状态码。
	/// </summary>
	public int StatusCode { get; } = statusCode;

	/// <summary>
	/// 转换为 ProblemDetails 时使用的错误标题。
	/// </summary>
	public string ProblemTitle { get; } = problemTitle;
}

/// <summary>
/// 表示在线日志命令执行过程中出现的业务失败。
/// </summary>
public sealed class OnlineLogCommandException(int statusCode, string message)
	: ServiceCommandException(statusCode, GetProblemTitle(statusCode), message) {
	/// <summary>
	/// 获取在线日志创建失败对应的错误标题。
	/// </summary>
	/// <param name="statusCode">HTTP 状态码</param>
	/// <returns>用于 ProblemDetails 的错误标题</returns>
	private static string GetProblemTitle(int statusCode) => statusCode switch {
		StatusCodes.Status400BadRequest => "设备日志请求无效",
		StatusCodes.Status403Forbidden => "不允许写入日志",
		StatusCodes.Status404NotFound => "目标设备不存在",
		_ => "日志写入失败"
	};
}

/// <summary>
/// 表示用户与 API 凭据管理命令执行过程中出现的业务失败。
/// </summary>
public sealed class UserManagementCommandException(int statusCode, string message)
	: ServiceCommandException(statusCode, GetProblemTitle(statusCode), message) {
	/// <summary>
	/// 获取用户与 API 凭据管理失败对应的错误标题。
	/// </summary>
	/// <param name="statusCode">HTTP 状态码</param>
	/// <returns>用于 ProblemDetails 的错误标题</returns>
	private static string GetProblemTitle(int statusCode) => statusCode switch {
		StatusCodes.Status400BadRequest => "用户管理请求无效",
		StatusCodes.Status404NotFound => "目标用户或 API 凭据不存在",
		StatusCodes.Status409Conflict => "用户管理状态冲突",
		StatusCodes.Status422UnprocessableEntity => "用户管理请求语义无效",
		_ => "用户管理失败"
	};
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

/// <summary>
/// 表示设备管理命令执行过程中出现的业务失败。
/// </summary>
public sealed class DeviceManagementCommandException(int statusCode, string message)
	: ServiceCommandException(statusCode, GetProblemTitle(statusCode), message) {
	/// <summary>
	/// 获取设备管理失败对应的错误标题。
	/// </summary>
	/// <param name="statusCode">HTTP 状态码</param>
	/// <returns>用于 ProblemDetails 的错误标题</returns>
	private static string GetProblemTitle(int statusCode) => statusCode switch {
		StatusCodes.Status400BadRequest => "设备管理请求无效",
		StatusCodes.Status404NotFound => "目标设备不存在",
		StatusCodes.Status409Conflict => "设备管理状态冲突",
		StatusCodes.Status422UnprocessableEntity => "设备管理请求语义无效",
		_ => "设备管理失败"
	};
}