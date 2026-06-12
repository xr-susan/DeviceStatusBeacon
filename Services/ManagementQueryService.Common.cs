namespace DeviceStatusBeacon.Services;

public sealed partial class ManagementQueryService {
	/// <summary>
	/// 归一化设备名称。
	/// </summary>
	/// <param name="value">原始设备名称</param>
	/// <returns>归一化后的设备名称；如果结果为空，则返回 null</returns>
	private string? NormalizeDeviceName(string? value) =>
		string.IsNullOrWhiteSpace(value)
			? null
			: lookupNormalizer.NormalizeName(value.Trim());

	/// <summary>
	/// 规范化用于显示名称匹配的文本。
	/// </summary>
	/// <param name="value">原始显示名称输入</param>
	/// <returns>去掉首尾空白后的文本；如果结果为空，则返回 null</returns>
	private static string? NormalizeDisplayNameSearchTerm(string? value) =>
		string.IsNullOrWhiteSpace(value)
			? null
			: value.Trim();

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