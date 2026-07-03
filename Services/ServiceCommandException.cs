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