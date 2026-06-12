namespace DeviceStatusBeacon.Extensions;

/// <summary>
/// 为 <see cref="HttpContext"/> 提供签名认证实体读取相关的扩展方法。
/// </summary>
public static class HttpContextAuthenticationExtensions {
	/// <summary>
	/// 在 <see cref="HttpContext.Items"/> 中存储当前请求在签名认证阶段缓存的实体时使用的键
	/// </summary>
	internal const string AuthenticatedEntityItemKey = $"{AuthenticationSchemeNames.Signature}.AuthenticatedEntity";

	/// <summary>
	/// 为 <see cref="HttpContext"/> 提供签名认证实体读取相关的扩展方法组
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	extension(HttpContext context) {
		/// <summary>
		/// 尝试读取当前请求在签名认证阶段缓存的实体。
		/// </summary>
		/// <returns>如果当前请求存在签名认证实体，则返回对应实体；否则返回 null</returns>
		public IHasProtectedSecretKey? GetAuthenticatedSignatureEntity() =>
			context.Items.TryGetValue(AuthenticatedEntityItemKey, out var entity)
				? entity as IHasProtectedSecretKey
				: null;

		/// <summary>
		/// 尝试读取当前请求在签名认证阶段缓存的指定类型实体。
		/// </summary>
		/// <typeparam name="T">目标实体类型</typeparam>
		/// <returns>如果当前请求存在指定类型的签名认证实体，则返回对应实体；否则返回 null</returns>
		public T? GetAuthenticatedSignatureEntity<T>() where T : class, IHasProtectedSecretKey =>
			context.Items.TryGetValue(AuthenticatedEntityItemKey, out var entity)
				? entity as T
				: null;
	}
}