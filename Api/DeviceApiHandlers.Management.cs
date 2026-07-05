namespace DeviceStatusBeacon.Api;

internal static partial class DeviceApiHandlers {
	/// <summary>
	/// 创建新设备。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="request">设备创建请求</param>
	/// <param name="deviceAdministrationService">设备管理服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>创建成功结果或统一错误响应</returns>
	public static async Task<IResult> CreateDeviceAsync(
		HttpContext context,
		CreateDeviceCommand request,
		IDeviceAdministrationService deviceAdministrationService,
		CancellationToken cancellationToken) {
		try {
			var commandResult = await deviceAdministrationService.CreateAsync(request, cancellationToken);
			return Results.CreatedAtRoute("GetDeviceById", new {
				deviceId = commandResult.DeviceId
			}, commandResult);
		} catch (DeviceAdministrationException e) {
			return ApiProblemResults.FromServiceException(context, e);
		}
	}

	/// <summary>
	/// 重置指定设备的操作密钥。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="deviceAdministrationService">设备管理服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>重置成功结果或统一错误响应</returns>
	public static async Task<IResult> ResetDeviceSecretKeyAsync(
		HttpContext context,
		Guid deviceId,
		IDeviceAdministrationService deviceAdministrationService,
		CancellationToken cancellationToken) {
		try {
			var commandResult = await deviceAdministrationService.ResetSecretKeyAsync(deviceId, cancellationToken);
			return Results.Ok(commandResult);
		} catch (DeviceAdministrationException e) {
			return ApiProblemResults.FromServiceException(context, e);
		}
	}

	/// <summary>
	/// 重命名指定设备。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="request">设备重命名请求</param>
	/// <param name="deviceAdministrationService">设备管理服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>重命名成功结果或统一错误响应</returns>
	public static async Task<IResult> RenameDeviceAsync(
		HttpContext context,
		Guid deviceId,
		RenameDeviceCommand request,
		IDeviceAdministrationService deviceAdministrationService,
		CancellationToken cancellationToken) {
		try {
			await deviceAdministrationService.RenameAsync(deviceId, request, cancellationToken);
			return Results.NoContent();
		} catch (DeviceAdministrationException e) {
			return ApiProblemResults.FromServiceException(context, e);
		}
	}

	/// <summary>
	/// 更新指定设备的显示名称。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="request">显示名称更新请求</param>
	/// <param name="deviceAdministrationService">设备管理服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>更新成功结果或统一错误响应</returns>
	public static async Task<IResult> SetDeviceDisplayNameAsync(
		HttpContext context,
		Guid deviceId,
		SetDeviceDisplayNameCommand request,
		IDeviceAdministrationService deviceAdministrationService,
		CancellationToken cancellationToken) {
		try {
			await deviceAdministrationService.SetDisplayNameAsync(deviceId, request, cancellationToken);
			return Results.NoContent();
		} catch (DeviceAdministrationException e) {
			return ApiProblemResults.FromServiceException(context, e);
		}
	}

	/// <summary>
	/// 更新指定设备的启用状态。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="request">启用状态更新请求</param>
	/// <param name="deviceAdministrationService">设备管理服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>更新成功结果或统一错误响应</returns>
	public static async Task<IResult> SetDeviceEnabledAsync(
		HttpContext context,
		Guid deviceId,
		SetDeviceEnabledCommand request,
		IDeviceAdministrationService deviceAdministrationService,
		CancellationToken cancellationToken) {
		try {
			await deviceAdministrationService.SetEnabledAsync(deviceId, request, cancellationToken);
			return Results.NoContent();
		} catch (DeviceAdministrationException e) {
			return ApiProblemResults.FromServiceException(context, e);
		}
	}

	/// <summary>
	/// 删除指定设备。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="deviceAdministrationService">设备管理服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>删除成功结果或统一错误响应</returns>
	public static async Task<IResult> DeleteDeviceAsync(
		HttpContext context,
		Guid deviceId,
		IDeviceAdministrationService deviceAdministrationService,
		CancellationToken cancellationToken) {
		try {
			await deviceAdministrationService.DeleteAsync(deviceId, cancellationToken);
			return Results.NoContent();
		} catch (DeviceAdministrationException e) {
			return ApiProblemResults.FromServiceException(context, e);
		}
	}
}