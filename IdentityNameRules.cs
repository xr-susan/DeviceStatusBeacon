using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DeviceStatusBeacon;

/// <summary>
/// 身份标识名称规则。
/// </summary>
public static partial class IdentityNameRules {
	/// <summary>
	/// 身份标识名称允许的最小长度。
	/// </summary>
	public const int MinimumLength = 4;

	/// <summary>
	/// 身份标识名称允许的最大长度。
	/// </summary>
	public const int MaximumLength = 64;

	/// <summary>
	/// 检查身份标识名称是否符合格式。
	/// </summary>
	/// <param name="value">待检查的身份标识名称</param>
	/// <returns>如果身份标识名称符合规则，则返回 true；否则返回 false</returns>
	public static bool IsValid([NotNullWhen(true)] string? value) =>
		// 预编译的正则表达式内已有最小长度检查，因此无需额外检查最小长度
		value?.Length <= MaximumLength
		&& IdentityNameRegex().IsMatch(value);

	/// <summary>
	/// 身份标识名称格式正则表达式。
	/// </summary>
	/// <returns>身份标识名称格式正则表达式</returns>
	[GeneratedRegex(@"^[A-Z\d][\-_A-Z\d]{2,62}[A-Z\d]$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex IdentityNameRegex();
}