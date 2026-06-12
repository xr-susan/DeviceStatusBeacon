using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Errors;

/// <summary>
/// HTTP 状态码页模型
/// </summary>
[AllowAnonymous]
[ResponseCache(NoStore = true)]
[IgnoreAntiforgeryToken]
public class StatusCodeModel : PageModel {
	/// <summary>
	/// 原始状态码
	/// </summary>
	public int OriginalStatusCode { get; private set; }

	/// <summary>
	/// 请求标识
	/// </summary>
	public string RequestId { get; private set; } = string.Empty;

	/// <summary>
	/// 原始请求路径与查询参数
	/// </summary>
	public string? OriginalPathAndQuery { get; private set; }

	/// <summary>
	/// 页面标题
	/// </summary>
	public string PageTitle { get; private set; } = string.Empty;

	/// <summary>
	/// 页面描述
	/// </summary>
	public string Description { get; private set; } = string.Empty;

	/// <summary>
	/// 处理 GET 状态码页请求
	/// </summary>
	public IActionResult OnGet(int statusCode) => ProcessStatusCode(statusCode);

	/// <summary>
	/// 处理 POST 状态码页请求
	/// </summary>
	public IActionResult OnPost(int statusCode) => ProcessStatusCode(statusCode);

	/// <summary>
	/// 处理 PUT 状态码页请求
	/// </summary>
	public IActionResult OnPut(int statusCode) => ProcessStatusCode(statusCode);

	/// <summary>
	/// 处理 DELETE 状态码页请求
	/// </summary>
	public IActionResult OnDelete(int statusCode) => ProcessStatusCode(statusCode);

	/// <summary>
	/// 处理 PATCH 状态码页请求
	/// </summary>
	public IActionResult OnPatch(int statusCode) => ProcessStatusCode(statusCode);

	/// <summary>
	/// 处理 HEAD 状态码页请求
	/// </summary>
	public IActionResult OnHead(int statusCode) => ProcessStatusCode(statusCode);

	/// <summary>
	/// 处理 OPTIONS 状态码页请求
	/// </summary>
	public IActionResult OnOptions(int statusCode) => ProcessStatusCode(statusCode);

	/// <summary>
	/// 处理状态码页请求并按协商结果输出 HTML、JSON 或空响应
	/// </summary>
	/// <param name="statusCode">要呈现的状态码</param>
	/// <returns>当前请求对应的页面结果或内容结果</returns>
	private IActionResult ProcessStatusCode(int statusCode) {
		RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

		var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

		// 用户主动访问内部状态码页时，不认定为合法流程，一律转成 404。
		if (statusCodeReExecuteFeature is null) {
			Response.StatusCode = StatusCodes.Status404NotFound;
			OriginalStatusCode = StatusCodes.Status404NotFound;
			OriginalPathAndQuery = $"{Request.PathBase}{Request.Path}{Request.QueryString}";
		} else {
			Response.StatusCode = statusCode;
			OriginalStatusCode = statusCode;
			OriginalPathAndQuery = $"{statusCodeReExecuteFeature.OriginalPathBase}{statusCodeReExecuteFeature.OriginalPath}{statusCodeReExecuteFeature.OriginalQueryString}";
		}

		// 根据状态码获取页面标题和描述文案
		var (title, detail) = ErrorPageHelper.GetStatusPresentation(OriginalStatusCode);
		PageTitle = title;
		Description = detail;

		// 根据原始请求路径和当前请求头协商失败响应输出模式
		var originalPath = statusCodeReExecuteFeature?.OriginalPath ?? Request.Path;
		var responseMode = Request.GetFailureResponseMode(originalPath);

		// 根据协商结果返回 HTML 页面、Problem+Json 错误详情或空响应
		return responseMode switch {
			FailureResponseMode.Json => new ObjectResult(ErrorPageHelper.CreateProblemDetails(
				HttpContext,
				OriginalStatusCode,
				PageTitle,
				Description,
				OriginalPathAndQuery)) {
				StatusCode = OriginalStatusCode
			},
			FailureResponseMode.Empty => ErrorPageHelper.SuppressStatusCodePagesAndReturnEmptyResult(HttpContext),
			_ => Page()
		};
	}
}