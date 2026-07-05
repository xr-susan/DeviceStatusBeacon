using System.Globalization;

namespace DeviceStatusBeacon.Routing;

/// <summary>
/// 身份标识名称路由约束。
/// </summary>
public sealed class IdentityNameRouteConstraint : IRouteConstraint {
	/// <inheritdoc/>
	public bool Match(
		HttpContext? httpContext,
		IRouter? route,
		string routeKey,
		RouteValueDictionary values,
		RouteDirection routeDirection) =>
		values.TryGetValue(routeKey, out var value)
		&& IdentityNameRules.IsValid(Convert.ToString(value, CultureInfo.InvariantCulture));
}