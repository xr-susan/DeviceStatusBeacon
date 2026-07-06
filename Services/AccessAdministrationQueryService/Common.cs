using Microsoft.AspNetCore.Identity;

namespace DeviceStatusBeacon.Services;

/// <summary>
/// 访问管理查询服务。
/// </summary>
public sealed partial class AccessAdministrationQueryService(
	DeviceStatusBeaconContext dbContext,
	ILookupNormalizer lookupNormalizer) : IAccessAdministrationQueryService {
	/// <summary>
	/// 访问管理查询片段允许的最大数量。
	/// </summary>
	internal const int MaxAccessQueryCount = 50;

	/// <summary>
	/// 将访问管理分页数量约束到允许值。
	/// </summary>
	/// <param name="value">原始分页数量</param>
	/// <returns>归一化后的分页数量</returns>
	private static int NormalizePageSize(int value) =>
		Math.Clamp(value, 1, MaxAccessQueryCount);

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