using System.Text.RegularExpressions;

namespace DeviceStatusBeacon;

public static partial class GeneratedRegex {
	/// <summary>
	/// 获取源生成的身份标识正则表达式
	/// </summary>
	/// <returns>身份标识正则表达式</returns>
	[GeneratedRegex(@"^[A-Za-z\d][\-A-Za-z\d]{2,62}[A-Za-z\d]$", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
	public static partial Regex IdentityRegex();
}