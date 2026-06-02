using DeviceStatusBeacon.Services;

namespace DeviceStatusBeacon.Extensions;

/// <summary>
/// Minimal API 端点注册扩展方法。
/// </summary>
public static class EndpointRouteBuilderExtensions {
	/// <summary>
	/// 为 <see cref="IEndpointRouteBuilder"/> 提供 Minimal API 端点注册相关的扩展方法组
	/// </summary>
	/// <param name="endpoints">当前应用正在配置的端点路由构建器</param>
	extension(IEndpointRouteBuilder endpoints) {
		/// <summary>
		/// 注册应用 Minimal API 端点。
		/// </summary>
		/// <returns>当前端点构建器</returns>
		public IEndpointRouteBuilder MapApplicationApiEndpoints() {
			endpoints.MapDashboardApiEndpoints();
			return endpoints;
		}

		/// <summary>
		/// 注册 Dashboard 相关 Minimal API 端点。
		/// </summary>
		/// <returns>当前端点构建器</returns>
		public IEndpointRouteBuilder MapDashboardApiEndpoints() {
			var group = endpoints.MapGroup("/api/dashboard")
				.RequireAuthorization()
				.WithTags("Dashboard");

			group.MapGet("/activity",
				async (HttpContext context, IManagementQueryService queryService, CancellationToken cancellationToken) =>
					Results.Ok(await queryService.GetDashboardActivityAsync(context.User, cancellationToken)))
				.RequireAuthorization(AuthorizationPolicyNames.InteractiveUser);

			return endpoints;
		}
	}
}