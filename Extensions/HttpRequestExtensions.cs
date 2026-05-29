using System.Net.Mime;

namespace DeviceStatusBeacon.Extensions;

/// <summary>
/// 错误响应输出模式
/// </summary>
internal enum ErrorResponseMode {
	/// <summary>
	/// 输出友好 HTML 页面，或在认证挑战时允许浏览器重定向到登录页
	/// </summary>
	Html,

	/// <summary>
	/// 输出机器可读的 JSON 错误正文
	/// </summary>
	Json,

	/// <summary>
	/// 仅返回状态码，不附带正文；调用方应确保最终状态码不是 200
	/// </summary>
	Empty
}

/// <summary>
/// 为 <see cref="HttpRequest"/> 提供错误响应协商相关的扩展方法组
/// </summary>
internal static class HttpRequestExtensions {
	extension(HttpRequest request) {
		/// <summary>
		/// 基于指定目标路径和当前请求头判断错误响应输出模式，指定目标路径默认为当前请求路径
		/// </summary>
		/// <remarks>
		/// 异常页和状态码页重执行时，应传入原始请求路径，而不是当前内部页面路径
		/// </remarks>
		/// <param name="requestPath">要判定的目标路径；如果为 null，则使用当前请求路径</param>
		/// <returns>错误响应输出模式</returns>
		public ErrorResponseMode GetErrorResponseMode(PathString? requestPath = null) {
			requestPath ??= request.Path;

			// API 路径固定返回 JSON，保证调用方总能拿到稳定的机器可读错误格式
			if (requestPath?.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) == true) {
				return ErrorResponseMode.Json;
			}

			var acceptHeaders = request.GetTypedHeaders().Accept;

			// 对非 API 路径，如果请求方没有声明接受 HTML 类响应，只给状态码，不返回站点页面
			if (acceptHeaders is null || acceptHeaders.Count == 0) {
				return ErrorResponseMode.Empty;
			}

			// 站点页面统一按 HTML 文档输出处理，这里只认 text/html、text/* 和 */*
			foreach (var acceptedMediaType in acceptHeaders) {
				var mediaType = acceptedMediaType.MediaType.Value;

				// 预先判断 null 或全空白字符串
				if (string.IsNullOrWhiteSpace(mediaType)) {
					continue;
				}

				if (mediaType.Equals(MediaTypeNames.Text.Html, StringComparison.OrdinalIgnoreCase)
					|| mediaType.Equals("text/*", StringComparison.OrdinalIgnoreCase)
					|| mediaType.Equals("*/*", StringComparison.OrdinalIgnoreCase)) {
					return ErrorResponseMode.Html;
				}
			}

			return ErrorResponseMode.Empty;
		}
	}
}