namespace DeviceStatusBeacon.Pages;

/// <summary>
/// 每页数量选项辅助方法。
/// </summary>
internal static class PageSizeOptionHelper {
	/// <summary>
	/// 创建每页数量选项。
	/// </summary>
	/// <param name="fixedOptions">固定选项</param>
	/// <param name="currentPageSize">当前有效每页数量</param>
	/// <returns>每页数量选项</returns>
	public static int[] Create(int[] fixedOptions, int currentPageSize) =>
		fixedOptions.Contains(currentPageSize) ? fixedOptions : [.. fixedOptions, currentPageSize];
}