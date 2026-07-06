using Microsoft.AspNetCore.Identity;

namespace DeviceStatusBeacon;

/// <summary>
/// 已归一化的身份标识名称。
/// </summary>
/// <param name="NormalizedName">归一化后的身份标识名称</param>
public sealed record IdentityNameLookup(string NormalizedName) {
	/// <summary>
	/// 尝试从原始名称创建身份标识名称查找条件。
	/// </summary>
	/// <param name="value">原始身份标识名称</param>
	/// <param name="normalizer">身份标识名称归一化器</param>
	/// <returns>可用于查询的身份标识名称；如果原始名称不符合规则，则返回 null</returns>
	public static IdentityNameLookup? TryCreate(string? value, ILookupNormalizer normalizer) {
		ArgumentNullException.ThrowIfNull(normalizer);

		var trimmedValue = value?.Trim();
		return IdentityNameRules.IsValid(trimmedValue)
			? new(normalizer.NormalizeName(trimmedValue))
			: null;
	}

	/// <summary>
	/// 从已经验证过的身份标识名称创建查找条件。
	/// </summary>
	/// <param name="value">已经验证过的身份标识名称</param>
	/// <param name="normalizer">身份标识名称归一化器</param>
	/// <returns>可用于查询的身份标识名称</returns>
	public static IdentityNameLookup CreateFromValidName(string value, ILookupNormalizer normalizer) {
		ArgumentNullException.ThrowIfNull(normalizer);

		return new(normalizer.NormalizeName(value));
	}
}

/// <summary>
/// 身份标识搜索条件。
/// </summary>
/// <param name="NormalizedName">归一化后的身份标识名称搜索关键字</param>
/// <param name="DisplayName">显示名称搜索关键字</param>
public sealed record IdentitySearchTerm(
	string? NormalizedName,
	string? DisplayName
) {
	/// <summary>
	/// 从原始搜索关键字创建身份标识搜索条件。
	/// </summary>
	/// <param name="value">原始搜索关键字</param>
	/// <param name="normalizer">身份标识名称归一化器</param>
	/// <returns>身份标识搜索条件</returns>
	public static IdentitySearchTerm Create(string? value, ILookupNormalizer normalizer) {
		ArgumentNullException.ThrowIfNull(normalizer);

		if (string.IsNullOrWhiteSpace(value)) {
			return new(null, null);
		}

		var trimmedValue = value?.Trim();
		return new(
			normalizer.NormalizeName(trimmedValue),
			trimmedValue);
	}
}