namespace DeviceStatusBeacon.Api;

/// <summary>
/// 应用 Minimal API 总注册扩展方法。
/// </summary>
public static class ApplicationApiEndpointRouteBuilderExtensions {
	/// <summary>
	/// 为 <see cref="IEndpointRouteBuilder"/> 提供应用 Minimal API 总注册相关的扩展方法组
	/// </summary>
	/// <param name="endpoints">当前应用正在配置的端点路由构建器</param>
	extension(IEndpointRouteBuilder endpoints) {
		/// <summary>
		/// 注册应用的全部 Minimal API 端点。
		/// </summary>
		/// <returns>当前端点构建器</returns>
		public IEndpointRouteBuilder MapApplicationApiEndpoints() {
			endpoints.MapDashboardApiEndpoints();
			endpoints.MapDeviceApiEndpoints();
			endpoints.MapLogInternalApiEndpoints();
			endpoints.MapAgentInternalApiEndpoints();
			return endpoints;
		}
	}
}