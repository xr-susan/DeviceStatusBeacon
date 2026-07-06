using Microsoft.AspNetCore.Antiforgery;

namespace DeviceStatusBeacon.Api;

/// <summary>
/// Log 相关 Minimal API 处理逻辑。
/// </summary>
internal static class LogApiHandlers {
	/// <summary>
	/// 为指定设备名称创建在线日志。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceName">路由中指定的目标设备名称</param>
	/// <param name="request">在线日志创建请求</param>
	/// <param name="deviceStatusQueryService">设备状态查询服务</param>
	/// <param name="onlineLogManagementService">在线日志管理服务</param>
	/// <param name="antiforgery">防伪令牌服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>创建成功结果或统一错误响应</returns>
	public static async Task<IResult> CreateInternalOnlineLogAsync(
		HttpContext context,
		string deviceName,
		CreateOnlineLogCommand request,
		IDeviceStatusQueryService deviceStatusQueryService,
		IOnlineLogManagementService onlineLogManagementService,
		IAntiforgery antiforgery,
		CancellationToken cancellationToken) {
		try {
			await antiforgery.ValidateRequestAsync(context);

			var session = deviceStatusQueryService.CreateQuerySessionAsync(context.User);
			var device = await deviceStatusQueryService.GetDeviceByNameAsync(session, deviceName, cancellationToken);
			if (device is null) {
				return ApiProblemResults.DeviceNotFound(context);
			}

			var commandResult = await onlineLogManagementService.CreateAsync(
				device.DeviceId,
				request,
				context.Connection.RemoteIpAddress,
				session.PrincipalId,
				authenticatedDevice: null,
				cancellationToken);

			return Results.Ok(commandResult);
		} catch (AntiforgeryValidationException) {
			return ApiProblemResults.InvalidAntiforgeryToken(context);
		} catch (OnlineLogManagementException e) {
			return ApiProblemResults.FromServiceException(context, e);
		}
	}

	/// <summary>
	/// 获取单条在线日志详细信息。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="onlineLogId">在线日志 ID</param>
	/// <param name="deviceStatusQueryService">设备状态查询服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>在线日志详细信息或统一错误响应</returns>
	public static async Task<IResult> GetOnlineLogAsync(
		HttpContext context,
		long onlineLogId,
		IDeviceStatusQueryService deviceStatusQueryService,
		CancellationToken cancellationToken) {
		var onlineLog = await deviceStatusQueryService.GetOnlineLogDetailsAsync(context.User, onlineLogId, cancellationToken);

		return onlineLog is null
			? ApiProblemResults.OnlineLogNotFound(context)
			: Results.Ok(onlineLog);
	}

	/// <summary>
	/// 更新单条在线日志的附加消息。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="onlineLogId">在线日志 ID</param>
	/// <param name="request">在线日志消息更新请求</param>
	/// <param name="onlineLogManagementService">在线日志管理服务</param>
	/// <param name="antiforgery">防伪令牌服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>更新成功结果或统一错误响应</returns>
	public static async Task<IResult> UpdateOnlineLogMessageAsync(
		HttpContext context,
		long onlineLogId,
		UpdateOnlineLogMessageCommand request,
		IOnlineLogManagementService onlineLogManagementService,
		IAntiforgery antiforgery,
		CancellationToken cancellationToken) {
		try {
			await antiforgery.ValidateRequestAsync(context);
			await onlineLogManagementService.UpdateMessageAsync(onlineLogId, request, cancellationToken);
			return Results.NoContent();
		} catch (AntiforgeryValidationException) {
			return ApiProblemResults.InvalidAntiforgeryToken(context);
		} catch (OnlineLogManagementException e) {
			return ApiProblemResults.FromServiceException(context, e);
		}
	}

	/// <summary>
	/// 删除单条在线日志。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="onlineLogId">在线日志 ID</param>
	/// <param name="onlineLogManagementService">在线日志管理服务</param>
	/// <param name="antiforgery">防伪令牌服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>删除成功结果或统一错误响应</returns>
	public static async Task<IResult> DeleteOnlineLogAsync(
		HttpContext context,
		long onlineLogId,
		IOnlineLogManagementService onlineLogManagementService,
		IAntiforgery antiforgery,
		CancellationToken cancellationToken) {
		try {
			await antiforgery.ValidateRequestAsync(context);
			await onlineLogManagementService.DeleteAsync(onlineLogId, cancellationToken);
			return Results.NoContent();
		} catch (AntiforgeryValidationException) {
			return ApiProblemResults.InvalidAntiforgeryToken(context);
		} catch (OnlineLogManagementException e) {
			return ApiProblemResults.FromServiceException(context, e);
		}
	}
}