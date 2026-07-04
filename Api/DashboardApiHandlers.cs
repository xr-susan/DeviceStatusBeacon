namespace DeviceStatusBeacon.Api;

/// <summary>
/// Dashboard 相关 Minimal API 处理逻辑。
/// </summary>
internal static class DashboardApiHandlers {
	/// <summary>
	/// 获取 Dashboard 最近活动数据。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="queryService">管理查询服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>包装最近活动数据的 HTTP 结果</returns>
	public static async Task<IResult> GetActivityAsync(
		HttpContext context,
		IManagementQueryService queryService,
		CancellationToken cancellationToken) =>
		Results.Ok(await queryService.GetDashboardActivityAsync(context.User, cancellationToken));
}