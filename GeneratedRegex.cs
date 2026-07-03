using System.Text.RegularExpressions;

namespace DeviceStatusBeacon;

public static partial class GeneratedRegex {
	/// <inheritdoc/>
	[GeneratedRegex(@"^[A-Z\d][\-_A-Z\d]{2,62}[A-Z\d]$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	public static partial Regex IdentityNameRegex();
}