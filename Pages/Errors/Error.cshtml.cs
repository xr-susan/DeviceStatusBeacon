using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DeviceStatusBeacon.Pages.Errors;

/// <summary>
/// 未处理异常错误页模型
/// </summary>
[AllowAnonymous]
[ResponseCache(NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel {
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
	public string PageTitle { get; private set; } = "服务器内部错误";

	/// <summary>
	/// 页面描述
	/// </summary>
	public string Description { get; private set; } = "服务器处理当前请求时发生了未预期错误。";

	/// <summary>
	/// 处理 GET 错误页请求
	/// </summary>
	public IActionResult OnGet() => ProcessRequest();

	/// <summary>
	/// 处理 POST 错误页请求
	/// </summary>
	public IActionResult OnPost() => ProcessRequest();

	/// <summary>
	/// 处理 PUT 错误页请求
	/// </summary>
	public IActionResult OnPut() => ProcessRequest();

	/// <summary>
	/// 处理 DELETE 错误页请求
	/// </summary>
	public IActionResult OnDelete() => ProcessRequest();

	/// <summary>
	/// 处理 PATCH 错误页请求
	/// </summary>
	public IActionResult OnPatch() => ProcessRequest();

	/// <summary>
	/// 处理 HEAD 错误页请求
	/// </summary>
	public IActionResult OnHead() => ProcessRequest();

	/// <summary>
	/// 处理 OPTIONS 错误页请求
	/// </summary>
	public IActionResult OnOptions() => ProcessRequest();

	/// <summary>
	/// 处理错误页请求并按协商结果输出 HTML、JSON 或空响应
	/// </summary>
	/// <returns>当前请求对应的页面结果或内容结果</returns>
	private IActionResult ProcessRequest() {
		var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

		// 如果连异常处理中间件写入的原始路径都不存在，说明不是管线内部转入，而是用户主动访问了该页。
		if (string.IsNullOrWhiteSpace(exceptionFeature?.Path)) {
			return NotFound();
		}

		RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
		Response.StatusCode = StatusCodes.Status500InternalServerError;
		OriginalPathAndQuery = $"{Request.PathBase}{exceptionFeature.Path}{Request.QueryString}";

		// 根据原始失败路径和当前请求头协商失败响应输出模式。
		var responseMode = Request.GetFailureResponseMode(exceptionFeature.Path);

		// 根据协商结果返回 HTML 页面、Problem+Json 错误详情或空响应。
		return responseMode switch {
			FailureResponseMode.Json => new ObjectResult(ErrorPageHelper.CreateProblemDetails(
				HttpContext,
				StatusCodes.Status500InternalServerError,
				PageTitle,
				Description,
				OriginalPathAndQuery)) {
				StatusCode = StatusCodes.Status500InternalServerError
			},
			FailureResponseMode.Empty => ErrorPageHelper.SuppressStatusCodePagesAndReturnEmptyResult(HttpContext),
			_ => Page()
		};
	}
}