using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace DeviceStatusBeacon.Services;

/// <summary>
/// 供 Web 管理页面与 CLI 管理命令共享的查询服务。
/// </summary>
/// <remarks>
/// 该服务把“查询范围判断”和“设备 / 日志读取”收敛到同一个地方：
/// Web 可以基于当前登录主体构建受限查询；
/// CLI 则可以显式创建特权查询会话，从而复用相同的投影、筛选和排序规则。
/// </remarks>
public sealed partial class ManagementQueryService(DeviceStatusBeaconContext dbContext, ILookupNormalizer lookupNormalizer) : IManagementQueryService {
	/// <summary>
	/// 首页摘要中展示的设备数量。
	/// </summary>
	private const int DashboardDeviceActivityCount = 8;

	/// <summary>
	/// Dashboard 中“近期活跃设备”统计使用的时间窗口（小时）。
	/// </summary>
	private const int DashboardRecentActiveWindowHours = 24;

	/// <summary>
	/// 设备查询允许的最大分页大小。
	/// </summary>
	private const int MaxDeviceQueryCount = 50;

	/// <summary>
	/// 日志查询允许的最大分页大小。
	/// </summary>
	private const int MaxLogQueryCount = 100;

	/// <inheritdoc/>
	public ManagementQuerySession CreateQuerySessionAsync(ClaimsPrincipal principal) {
		var (principalKind, principalId, role) = principal.GetAuthenticatedPrincipalInfo();
		var queryPrincipalKind = principalKind switch {
			PrincipalKind.User => ManagementQueryPrincipalKind.User,
			PrincipalKind.ApiCredential => ManagementQueryPrincipalKind.ApiCredential,
			_ => ManagementQueryPrincipalKind.Unknown
		};

		var userName = principal.Identity?.Name ?? string.Empty;
		var displayName = principal.FindFirstValue(ClaimTypes.GivenName);

		return new(
			PrincipalId: principalId,
			PrincipalKind: queryPrincipalKind,
			UserName: userName,
			DisplayName: displayName,
			Role: role);
	}

	/// <inheritdoc/>
	public ManagementQuerySession CreatePrivilegedQuerySession(string userName = "CLI") =>
		new(
			PrincipalId: null,
			PrincipalKind: ManagementQueryPrincipalKind.Privileged,
			UserName: userName,
			DisplayName: null,
			Role: PrincipalRole.Administrator);

	/// <summary>
	/// 将分页数量约束到指定区间。
	/// </summary>
	/// <param name="value">原始输入</param>
	/// <param name="minimum">允许的最小值</param>
	/// <param name="maximum">允许的最大值</param>
	/// <returns>归一化后的分页数量</returns>
	private static int NormalizePageSize(int value, int minimum, int maximum) => Math.Clamp(value, minimum, maximum);

	/// <summary>
	/// 将页码约束到有效范围。
	/// </summary>
	/// <param name="value">原始页码</param>
	/// <returns>归一化后的页码</returns>
	private static int NormalizePageNumber(int value) => Math.Max(1, value);

	/// <summary>
	/// 根据总数重新约束页码。
	/// </summary>
	/// <param name="pageNumber">已经归一化的页码</param>
	/// <param name="pageSize">已经归一化的每页数量</param>
	/// <param name="totalCount">匹配总数</param>
	/// <returns>不会超过最后一页的页码</returns>
	private static int NormalizePageNumberForTotalCount(int pageNumber, int pageSize, int totalCount) =>
		Math.Min(pageNumber, PaginationData.GetTotalPages(totalCount, pageSize));

	/// <summary>
	/// 计算分页查询需要跳过的数据量。
	/// </summary>
	/// <param name="pageNumber">已经归一化的页码</param>
	/// <param name="pageSize">已经归一化的每页数量</param>
	/// <returns>分页查询跳过的数据量</returns>
	private static int CalculateSkipCount(int pageNumber, int pageSize) => (pageNumber - 1) * pageSize;
}