namespace DeviceStatusBeacon.Services;

public sealed partial class ManagementQueryService {
	/// <summary>
	/// 规范化筛选关键字。
	/// </summary>
	/// <param name="value">原始输入</param>
	/// <returns>去掉首尾空白后的关键字；如果结果为空，则返回 null</returns>
	private static string? NormalizeSearchTerm(string? value) =>
		string.IsNullOrWhiteSpace(value)
			? null
			: value.Trim();

	/// <summary>
	/// 将返回条数约束到指定区间。
	/// </summary>
	/// <param name="value">原始输入</param>
	/// <param name="minimum">允许的最小值</param>
	/// <param name="maximum">允许的最大值</param>
	/// <returns>归一化后的返回条数</returns>
	private static int NormalizeTake(int value, int minimum, int maximum) => Math.Clamp(value, minimum, maximum);
}