using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DeviceStatusBeacon.Pages.Errors;

/// <summary>
/// 为错误页和状态码页提供共用帮助方法
/// </summary>
internal static class ErrorPageHelper {
	/// <summary>
	/// 获取未处理异常对应的 HTTP 状态码。
	/// </summary>
	/// <param name="exception">异常处理中间件捕获到的异常</param>
	/// <returns>应当返回给客户端的 HTTP 状态码</returns>
	public static int GetExceptionStatusCode(Exception? exception) =>
		exception switch {
			BadHttpRequestException badHttpRequestException => badHttpRequestException.StatusCode,
			_ => StatusCodes.Status500InternalServerError
		};

	/// <summary>
	/// 创建用于错误响应的 <see cref="ProblemDetails"/>
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="statusCode">错误状态码</param>
	/// <param name="title">错误标题</param>
	/// <param name="detail">错误详情</param>
	/// <param name="instance">出错请求实例</param>
	/// <returns>构造完成的 <see cref="ProblemDetails"/></returns>
	public static ProblemDetails CreateProblemDetails(HttpContext context, int statusCode, string title, string detail, string? instance) {
		var problemDetails = new ProblemDetails {
			Status = statusCode,
			Title = title,
			Detail = detail,
			Instance = string.IsNullOrWhiteSpace(instance) ? null : instance
		};

		// traceId 用于把页面 / JSON 返回的错误与服务端日志中的同一次请求对应起来
		problemDetails.Extensions["traceId"] = context.TraceIdentifier;
		return problemDetails;
	}

	/// <summary>
	/// 获取状态码页使用的标题和描述文案
	/// </summary>
	/// <param name="statusCode">HTTP 状态码</param>
	/// <returns>标题和描述文案</returns>
	public static (string Title, string Detail) GetStatusPresentation(int statusCode) => statusCode switch {
		StatusCodes.Status400BadRequest => ("请求无效", "当前请求的参数、格式或内容不符合要求。"),
		StatusCodes.Status401Unauthorized => ("需要登录", "访问当前页面或资源前需要先完成登录。"),
		StatusCodes.Status403Forbidden => ("拒绝访问", "当前账户没有权限访问该功能或资源。"),
		StatusCodes.Status404NotFound => ("页面不存在", "未找到对应页面或资源，地址可能已失效或尚未发布。"),
		StatusCodes.Status405MethodNotAllowed => ("请求方式不支持", "当前入口不支持这类 HTTP 请求方式。"),
		StatusCodes.Status409Conflict => ("操作状态冲突", "当前资源状态与请求操作不匹配，请刷新后重试。"),
		StatusCodes.Status422UnprocessableEntity => ("请求语义无效", "请求内容可以解析，但不符合当前业务规则。"),
		StatusCodes.Status500InternalServerError => ("服务器内部错误", "服务器处理当前请求时发生未预期错误，请保留请求标识以便排查。"),
		> 500 and < 600 => ("服务器内部错误", "服务器处理当前请求时发生未预期错误，请保留请求标识以便排查。"),
		_ => ($"状态码 {statusCode}", "服务器返回了一个未单独适配的 HTTP 状态码。")
	};

	/// <summary>
	/// 为当前请求关闭状态码页重执行，并返回一个空结果
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <returns>一个空结果</returns>
	public static EmptyResult SuppressStatusCodePagesAndReturnEmptyResult(HttpContext context) {
		var statusCodePagesFeature = context.Features.Get<IStatusCodePagesFeature>();
		// 当前请求已经落到目标状态码页中，若决定空输出，则要阻止状态码页中间件再次介入
		statusCodePagesFeature?.Enabled = false;
		return new EmptyResult();
	}
}