namespace DeviceStatusBeacon.Api;

/// <summary>
/// Log 分类 Minimal API 注册扩展方法。
/// </summary>
public static class LogApiEndpointRouteBuilderExtensions {
	/// <summary>
	/// 为 <see cref="IEndpointRouteBuilder"/> 提供 Log 分类 Minimal API 注册相关的扩展方法组
	/// </summary>
	/// <param name="endpoints">当前应用正在配置的端点路由构建器</param>
	extension(IEndpointRouteBuilder endpoints) {
		/// <summary>
		/// 注册 Log 相关内部 Minimal API 端点。
		/// </summary>
		/// <returns>当前端点构建器</returns>
		public IEndpointRouteBuilder MapLogInternalApiEndpoints() {
			var group = endpoints.MapGroup("/api/internal/logs")
				.WithTags("Logs");

			group.MapGet("/{onlineLogId:long}", LogApiHandlers.GetOnlineLogAsync)
				.RequireAuthorization(AuthorizationPolicyNames.InteractiveUser)
				.WithName("GetOnlineLog");

			group.MapPut("/{onlineLogId:long}/message", LogApiHandlers.UpdateOnlineLogMessageAsync)
				.RequireAuthorization(AuthorizationPolicyNames.InteractiveDeviceManagement)
				.WithName("UpdateOnlineLogMessage");

			group.MapDelete("/{onlineLogId:long}", LogApiHandlers.DeleteOnlineLogAsync)
				.RequireAuthorization(AuthorizationPolicyNames.InteractiveDeviceManagement)
				.WithName("DeleteOnlineLog");

			return endpoints;
		}
	}
}