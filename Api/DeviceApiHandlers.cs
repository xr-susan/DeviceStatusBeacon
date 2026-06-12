using DeviceStatusBeacon.Pages.Errors;
using DeviceStatusBeacon.Services;

namespace DeviceStatusBeacon.Api;

/// <summary>
/// Device 相关 Minimal API 处理逻辑。
/// </summary>
internal static class DeviceApiHandlers {
	/// <summary>
	/// 为指定设备创建在线日志。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceId">路由中指定的目标设备 ID</param>
	/// <param name="request">在线日志创建请求</param>
	/// <param name="onlineLogCommandService">在线日志命令服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>创建成功结果或统一错误响应</returns>
	public static async Task<IResult> CreateOnlineLogAsync(
		HttpContext context,
		Guid deviceId,
		CreateOnlineLogCommand request,
		IOnlineLogCommandService onlineLogCommandService,
		CancellationToken cancellationToken) {
		// 从当前请求的认证信息中尝试获取已认证的设备实体或提交用户 ID
		var authenticatedDevice = context.GetAuthenticatedSignatureEntity<Device>();
		var submittedByUserId = authenticatedDevice is null
			? context.GetAuthenticatedSignatureEntity<ApiCredential>()?.UserId ?? context.User.TryReadUserId() // 兼容通过 Identity Cookie 认证的用户主体
			: null;

		try {
			var commandResult = await onlineLogCommandService.CreateAsync(
				deviceId,
				request,
				context.Connection.RemoteIpAddress,
				submittedByUserId,
				authenticatedDevice,
				cancellationToken);

			return Results.Ok(commandResult);
		} catch (OnlineLogCommandException e) {
			// 在线日志命令服务会把常见业务失败统一转成带状态码的异常；
			// Minimal API 终结点在这里再将其转换为一致的 ProblemDetails JSON 响应。
			return Results.Problem(ErrorPageHelper.CreateProblemDetails(
				context,
				e.StatusCode,
				GetCreateOnlineLogErrorTitle(e.StatusCode),
				e.Message,
				$"{context.Request.Path}{context.Request.QueryString}"));
		}
	}

	/// <summary>
	/// 获取在线日志创建失败对应的错误标题。
	/// </summary>
	/// <param name="statusCode">HTTP 状态码</param>
	/// <returns>用于 ProblemDetails 的错误标题</returns>
	private static string GetCreateOnlineLogErrorTitle(int statusCode) => statusCode switch {
		StatusCodes.Status400BadRequest => "设备日志请求无效",
		StatusCodes.Status403Forbidden => "不允许写入日志",
		StatusCodes.Status404NotFound => "目标设备不存在",
		_ => "日志写入失败"
	};
}