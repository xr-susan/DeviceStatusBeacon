namespace DeviceStatusBeacon.Services;

/// <summary>
/// 表示服务层业务操作过程中出现的业务失败。
/// </summary>
public abstract class ServiceException(int statusCode, string problemTitle, string message) : Exception(message) {
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
/// 表示在线日志管理操作过程中出现的业务失败。
/// </summary>
public sealed class OnlineLogManagementException(int statusCode, string message)
	: ServiceException(statusCode, GetProblemTitle(statusCode), message) {
	/// <summary>
	/// 获取在线日志管理失败对应的错误标题。
	/// </summary>
	/// <param name="statusCode">HTTP 状态码</param>
	/// <returns>用于 ProblemDetails 的错误标题</returns>
	private static string GetProblemTitle(int statusCode) => statusCode switch {
		StatusCodes.Status400BadRequest => "设备日志请求无效",
		StatusCodes.Status403Forbidden => "不允许操作日志",
		StatusCodes.Status404NotFound => "目标设备不存在",
		_ => "日志管理失败"
	};
}

/// <summary>
/// 表示用户与 API 凭据管理操作过程中出现的业务失败。
/// </summary>
public sealed class AccessAdministrationException(int statusCode, string message)
	: ServiceException(statusCode, GetProblemTitle(statusCode), message) {
	/// <summary>
	/// 获取用户与 API 凭据管理失败对应的错误标题。
	/// </summary>
	/// <param name="statusCode">HTTP 状态码</param>
	/// <returns>用于 ProblemDetails 的错误标题</returns>
	private static string GetProblemTitle(int statusCode) => statusCode switch {
		StatusCodes.Status400BadRequest => "用户或 API 凭据管理请求无效",
		StatusCodes.Status404NotFound => "目标用户或 API 凭据不存在",
		StatusCodes.Status409Conflict => "用户或 API 凭据管理状态冲突",
		StatusCodes.Status422UnprocessableEntity => "用户或 API 凭据管理请求语义无效",
		_ => "用户或 API 凭据管理失败"
	};
}

/// <summary>
/// 表示账号设置操作过程中出现的业务失败。
/// </summary>
public sealed class AccountSettingsException(int statusCode, string message)
	: ServiceException(statusCode, GetProblemTitle(statusCode), message) {
	/// <summary>
	/// 获取账号设置失败对应的错误标题。
	/// </summary>
	/// <param name="statusCode">HTTP 状态码</param>
	/// <returns>用于 ProblemDetails 的错误标题</returns>
	private static string GetProblemTitle(int statusCode) => statusCode switch {
		StatusCodes.Status400BadRequest => "账号设置请求无效",
		StatusCodes.Status401Unauthorized => "账号未登录",
		StatusCodes.Status403Forbidden => "账号无权执行该操作",
		StatusCodes.Status404NotFound => "目标资源不存在",
		StatusCodes.Status409Conflict => "账号设置状态冲突",
		StatusCodes.Status422UnprocessableEntity => "账号设置请求语义无效",
		_ => "账号设置失败"
	};
}

/// <summary>
/// 表示设备管理操作过程中出现的业务失败。
/// </summary>
public sealed class DeviceAdministrationException(int statusCode, string message)
	: ServiceException(statusCode, GetProblemTitle(statusCode), message) {
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