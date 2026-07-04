namespace DeviceStatusBeacon.Api;

internal static partial class DeviceApiHandlers {
	/// <summary>
	/// 获取当前主体可读取的设备列表。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="queryService">管理查询服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <param name="q">设备名称或显示名称筛选关键字</param>
	/// <param name="page">页码</param>
	/// <param name="pageSize">每页数量</param>
	/// <returns>设备列表 API 响应</returns>
	public static async Task<IResult> GetDevicesAsync(
		HttpContext context,
		IManagementQueryService queryService,
		CancellationToken cancellationToken,
		string? q = null,
		int page = 1,
		int pageSize = 20) {
		var devices = await queryService.GetDevicesAsync(
			context.User,
			q,
			page,
			pageSize,
			cancellationToken);

		return Results.Ok(new DeviceListApiResponse(
			devices.Pagination,
			devices.Devices));
	}

	/// <summary>
	/// 按设备 ID 获取单个设备详情。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="queryService">管理查询服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>设备详情 API 响应或统一错误响应</returns>
	public static async Task<IResult> GetDeviceByIdAsync(
		HttpContext context,
		Guid deviceId,
		IManagementQueryService queryService,
		CancellationToken cancellationToken) {
		var session = queryService.CreateQuerySessionAsync(context.User);
		var deviceDetails = await queryService.GetDeviceDetailsByIdAsync(
			session,
			deviceId,
			cancellationToken);

		return deviceDetails is null ? ApiProblemResults.DeviceNotFound(context) : Results.Ok(deviceDetails);
	}

	/// <summary>
	/// 按设备名称获取单个设备详情。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="deviceName">设备名称</param>
	/// <param name="queryService">管理查询服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>设备详情 API 响应或统一错误响应</returns>
	public static async Task<IResult> GetDeviceByNameAsync(
		HttpContext context,
		string deviceName,
		IManagementQueryService queryService,
		CancellationToken cancellationToken) {
		var session = queryService.CreateQuerySessionAsync(context.User);
		var deviceDetails = await queryService.GetDeviceDetailsByNameAsync(
			session,
			deviceName,
			cancellationToken);

		return deviceDetails is null ? ApiProblemResults.DeviceNotFound(context) : Results.Ok(deviceDetails);
	}
}