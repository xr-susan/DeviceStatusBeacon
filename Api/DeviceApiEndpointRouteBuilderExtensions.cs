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
			var group = endpoints.MapGroup("/api/devices")
				.WithTags("Devices");

			// 创建设备端点，要求用户或 API 凭据具有设备管理权限
			group.MapPost("", DeviceApiHandlers.CreateDeviceAsync)
				.RequireAuthorization(AuthorizationPolicyNames.UserOrApiCredentialDeviceManagement)
				.WithName("CreateDevice");

			// 设备日志端点，要求用户或 API 凭据具有日志提交权限
			group.MapPost("/{deviceId:guid}/logs", DeviceApiHandlers.CreateOnlineLogAsync)
				.RequireAuthorization(AuthorizationPolicyNames.LogSubmission)
				.WithName("CreateDeviceOnlineLog");

			// 设备查询相关端点组，要求用户或 API 凭据具有查询访问权限
			var queryGroup = group.MapGroup("")
				.RequireAuthorization(AuthorizationPolicyNames.UserOrApiCredentialQueryAccess);

			queryGroup.MapGet("", DeviceApiHandlers.GetDevicesAsync)
				.WithName("GetDevices");

			queryGroup.MapGet("/{deviceId:guid}", DeviceApiHandlers.GetDeviceByIdAsync)
				.WithName("GetDeviceById");

			queryGroup.MapGet("/by-name/{deviceName:identityName}", DeviceApiHandlers.GetDeviceByNameAsync)
				.WithName("GetDeviceByName");

			// 设备管理相关端点组，要求用户或 API 凭据具有设备管理权限
			var managedDeviceGroup = group.MapGroup("/{deviceId:guid}")
				.RequireAuthorization(AuthorizationPolicyNames.UserOrApiCredentialDeviceManagement);

			managedDeviceGroup.MapPost("/reset-secret-key", DeviceApiHandlers.ResetDeviceSecretKeyAsync)
				.WithName("ResetDeviceSecretKey");

			managedDeviceGroup.MapPut("/name", DeviceApiHandlers.RenameDeviceAsync)
				.WithName("RenameDevice");

			managedDeviceGroup.MapPut("/display-name", DeviceApiHandlers.SetDeviceDisplayNameAsync)
				.WithName("SetDeviceDisplayName");

			managedDeviceGroup.MapPut("/enabled", DeviceApiHandlers.SetDeviceEnabledAsync)
				.WithName("SetDeviceEnabled");

			managedDeviceGroup.MapDelete("", DeviceApiHandlers.DeleteDeviceAsync)
				.WithName("DeleteDevice");

			return endpoints;
		}
	}
}