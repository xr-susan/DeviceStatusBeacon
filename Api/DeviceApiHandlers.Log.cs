namespace DeviceStatusBeacon.Api;

/// <summary>
/// Device 相关 Minimal API 处理逻辑。
/// </summary>
internal static partial class DeviceApiHandlers {
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
		// 尝试获取已认证的设备实体
		var authenticatedDevice = context.GetAuthenticatedSignatureEntity<Device>();

		// 签名式 API 凭据记录其绑定用户；交互式用户则从 ClaimsPrincipal 读取用户 ID
		Guid? submittedByUserId = null;
		if (authenticatedDevice is null) {
			submittedByUserId = context.GetAuthenticatedSignatureEntity<ApiCredential>()?.UserId
				?? context.User.GetAuthenticatedPrincipalInfo() switch {
					(PrincipalKind.User, var userId, _) => userId,
					_ => null
				};
		}

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
			// Minimal API 终结点在这里再将其转换为一致的 ProblemDetails JSON 响应
			return ApiProblemResults.FromServiceCommandException(context, e);
		}
	}
}