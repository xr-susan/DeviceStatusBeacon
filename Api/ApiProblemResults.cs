using DeviceStatusBeacon.Pages.Errors;

namespace DeviceStatusBeacon.Api;

/// <summary>
/// Minimal API ProblemDetails 响应帮助方法。
/// </summary>
internal static class ApiProblemResults {
	/// <summary>
	/// 将服务层业务异常转换为统一错误响应。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="exception">服务层业务异常</param>
	/// <returns>ProblemDetails JSON 响应</returns>
	public static IResult FromServiceException(HttpContext context, ServiceException exception) =>
		CreateProblem(context, exception.StatusCode, exception.ProblemTitle, exception.Message);

	/// <summary>
	/// 创建设备不存在的统一错误响应。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <returns>ProblemDetails JSON 响应</returns>
	public static IResult DeviceNotFound(HttpContext context) =>
		CreateProblem(context, StatusCodes.Status404NotFound, "目标设备不存在", "未找到指定的设备。");

	/// <summary>
	/// 创建统一 ProblemDetails JSON 响应。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="statusCode">HTTP 状态码</param>
	/// <param name="title">错误标题</param>
	/// <param name="detail">错误详情</param>
	/// <returns>ProblemDetails JSON 响应</returns>
	private static IResult CreateProblem(HttpContext context, int statusCode, string title, string detail) =>
		Results.Problem(ErrorPageHelper.CreateProblemDetails(
			context,
			statusCode,
			title,
			detail,
			$"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}"));
}