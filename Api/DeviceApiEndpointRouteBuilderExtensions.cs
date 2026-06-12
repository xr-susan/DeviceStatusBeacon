namespace DeviceStatusBeacon.Api;

/// <summary>
/// Device 分类 Minimal API 注册扩展方法。
/// </summary>
public static class DeviceApiEndpointRouteBuilderExtensions {
	/// <summary>
	/// 为 <see cref="IEndpointRouteBuilder"/> 提供 Device 分类 Minimal API 注册相关的扩展方法组
	/// </summary>
	/// <param name="endpoints">当前应用正在配置的端点路由构建器</param>
	extension(IEndpointRouteBuilder endpoints) {
		/// <summary>
		/// 注册 Device 相关 Minimal API 端点。
		/// </summary>
		/// <returns>当前端点构建器</returns>
		public IEndpointRouteBuilder MapDeviceApiEndpoints() {
			var group = endpoints.MapGroup("/api/devices/{deviceId:guid}")
				.WithTags("Devices");

			group.MapPost("/logs", DeviceApiHandlers.CreateOnlineLogAsync)
				.RequireAuthorization(AuthorizationPolicyNames.LogSubmission)
				.WithName("CreateDeviceOnlineLog");

			return endpoints;
		}
	}
}