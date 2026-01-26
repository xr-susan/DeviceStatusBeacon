namespace DeviceStatusBeacon.Services;
/// <summary>
/// 用于添加本项目的自定义服务到 <see cref="IServiceCollection"/> 中的扩展方法组
/// </summary>
public static class CustomServiceCollectionExtensions {
	/// <summary>
	/// 注册本项目中自定义的服务到 <see cref="IServiceCollection"/> 中
	/// </summary>
	/// <param name="services">将要添加服务的 <see cref="IServiceCollection"/></param>
	/// <returns>当前的 <see cref="IServiceCollection"/>，用于链式调用</returns>
	public static IServiceCollection AddCustomServices(this IServiceCollection services) =>
		services;
}