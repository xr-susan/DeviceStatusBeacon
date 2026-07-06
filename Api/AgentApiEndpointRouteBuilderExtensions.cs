namespace DeviceStatusBeacon.Api;

/// <summary>
/// Agent 分类 Minimal API 注册扩展方法。
/// </summary>
public static class AgentApiEndpointRouteBuilderExtensions {
	/// <summary>
	/// 为 <see cref="IEndpointRouteBuilder"/> 提供 Agent 分类 Minimal API 注册相关的扩展方法组
	/// </summary>
	/// <param name="endpoints">当前应用正在配置的端点路由构建器</param>
	extension(IEndpointRouteBuilder endpoints) {
		/// <summary>
		/// 注册 Agent 相关内部 Minimal API 端点。
		/// </summary>
		/// <returns>当前端点构建器</returns>
		public IEndpointRouteBuilder MapAgentInternalApiEndpoints() {
			var group = endpoints.MapGroup("/api/internal/agent")
				.RequireAuthorization(AuthorizationPolicyNames.InteractiveDeviceManagement)
				.WithTags("InternalAgent");

			group.MapPost("/chat", AgentApiHandlers.ChatAsync)
				.WithName("AgentChat");

			return endpoints;
		}
	}
}