namespace DeviceStatusBeacon.Api;

internal static partial class DeviceApiHandlers {
	/// <summary>
	/// 创建新设备。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="request">设备创建请求</param>
	/// <param name="deviceManagementService">设备管理服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>创建成功结果或统一错误响应</returns>
	public static async Task<IResult> CreateDeviceAsync(
		HttpContext context,
		CreateDeviceCommand request,
		IDeviceManagementService deviceManagementService,
		CancellationToken cancellationToken) {
		try {
			var commandResult = await deviceManagementService.CreateAsync(request, cancellationToken);
			return Results.CreatedAtRoute("GetDeviceById", new {
				deviceId = commandResult.DeviceId
			}, commandResult);
		} catch (DeviceManagementCommandException e) {
			return ApiProblemResults.FromServiceCommandException(context, e);
		}
	}

	/// <summary>
	/// 重置指定设备的操作密钥。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="deviceManagementService">设备管理服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>重置成功结果或统一错误响应</returns>
	public static async Task<IResult> ResetDeviceSecretKeyAsync(
		HttpContext context,
		Guid deviceId,
		IDeviceManagementService deviceManagementService,
		CancellationToken cancellationToken) {
		try {
			var commandResult = await deviceManagementService.ResetSecretKeyAsync(deviceId, cancellationToken);
			return Results.Ok(commandResult);
		} catch (DeviceManagementCommandException e) {
			return ApiProblemResults.FromServiceCommandException(context, e);
		}
	}

	/// <summary>
	/// 重命名指定设备。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="request">设备重命名请求</param>
	/// <param name="deviceManagementService">设备管理服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>重命名成功结果或统一错误响应</returns>
	public static async Task<IResult> RenameDeviceAsync(
		HttpContext context,
		Guid deviceId,
		RenameDeviceCommand request,
		IDeviceManagementService deviceManagementService,
		CancellationToken cancellationToken) {
		try {
			await deviceManagementService.RenameAsync(deviceId, request, cancellationToken);
			return Results.NoContent();
		} catch (DeviceManagementCommandException e) {
			return ApiProblemResults.FromServiceCommandException(context, e);
		}
	}

	/// <summary>
	/// 更新指定设备的显示名称。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="request">显示名称更新请求</param>
	/// <param name="deviceManagementService">设备管理服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>更新成功结果或统一错误响应</returns>
	public static async Task<IResult> SetDeviceDisplayNameAsync(
		HttpContext context,
		Guid deviceId,
		SetDeviceDisplayNameCommand request,
		IDeviceManagementService deviceManagementService,
		CancellationToken cancellationToken) {
		try {
			await deviceManagementService.SetDisplayNameAsync(deviceId, request, cancellationToken);
			return Results.NoContent();
		} catch (DeviceManagementCommandException e) {
			return ApiProblemResults.FromServiceCommandException(context, e);
		}
	}

	/// <summary>
	/// 更新指定设备的启用状态。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="request">启用状态更新请求</param>
	/// <param name="deviceManagementService">设备管理服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>更新成功结果或统一错误响应</returns>
	public static async Task<IResult> SetDeviceEnabledAsync(
		HttpContext context,
		Guid deviceId,
		SetDeviceEnabledCommand request,
		IDeviceManagementService deviceManagementService,
		CancellationToken cancellationToken) {
		try {
			await deviceManagementService.SetEnabledAsync(deviceId, request, cancellationToken);
			return Results.NoContent();
		} catch (DeviceManagementCommandException e) {
			return ApiProblemResults.FromServiceCommandException(context, e);
		}
	}

	/// <summary>
	/// 删除指定设备。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="deviceManagementService">设备管理服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>删除成功结果或统一错误响应</returns>
	public static async Task<IResult> DeleteDeviceAsync(
		HttpContext context,
		Guid deviceId,
		IDeviceManagementService deviceManagementService,
		CancellationToken cancellationToken) {
		try {
			await deviceManagementService.DeleteAsync(deviceId, cancellationToken);
			return Results.NoContent();
		} catch (DeviceManagementCommandException e) {
			return ApiProblemResults.FromServiceCommandException(context, e);
		}
	}
}