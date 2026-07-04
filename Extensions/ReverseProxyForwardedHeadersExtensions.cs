using Microsoft.AspNetCore.HttpOverrides;
using NetworkRange = System.Net.IPNetwork;

namespace DeviceStatusBeacon.Extensions;

/// <summary>
/// 反向代理转发请求头配置扩展方法。
/// </summary>
public static class ReverseProxyForwardedHeadersExtensions {
	private const string ConfigurationSectionName = "ReverseProxy";

	/// <summary>
	/// 为 <see cref="IServiceCollection"/> 提供反向代理转发请求头配置相关的扩展方法组
	/// </summary>
	/// <param name="services">将要写入配置的 <see cref="IServiceCollection"/></param>
	extension(IServiceCollection services) {
		/// <summary>
		/// 依据配置注册反向代理转发请求头处理选项。
		/// </summary>
		/// <param name="configuration">应用配置</param>
		/// <returns>当前的 <see cref="IServiceCollection"/>，用于链式调用</returns>
		/// <exception cref="InvalidOperationException">当反向代理配置无法解析时</exception>
		/// <exception cref="FormatException">当反向代理配置格式不正确时</exception>
		public IServiceCollection AddReverseProxyForwardedHeaders(IConfiguration configuration) {
			var reverseProxyConfiguration = GetReverseProxyConfiguration(configuration);
			services.AddSingleton(reverseProxyConfiguration);

			if (!reverseProxyConfiguration.Enabled) {
				// 未启用反向代理模式时保持 ASP.NET Core 默认行为，不额外注册转发请求头处理选项
				return services;
			}

			services.Configure<ForwardedHeadersOptions>(options => {
				options.ForwardLimit = reverseProxyConfiguration.ForwardLimit;
				options.ForwardedHeaders =
					ForwardedHeaders.XForwardedFor |
					ForwardedHeaders.XForwardedProto |
					ForwardedHeaders.XForwardedPrefix;

				// KnownIPNetworks 是转发请求头的信任边界；
				// 只有来自这些网段或 ASP.NET Core 默认可信代理的请求头才会被中间件采信
				foreach (var knownNetwork in reverseProxyConfiguration.KnownNetworks) {
					options.KnownIPNetworks.Add(knownNetwork);
				}

				if (reverseProxyConfiguration.AllowedHosts.Count > 0) {
					// 只有配置了外部 Host 白名单时才处理 X-Forwarded-Host，避免错误代理配置放大 Host 伪造风险
					options.ForwardedHeaders |= ForwardedHeaders.XForwardedHost;

					foreach (var allowedHost in reverseProxyConfiguration.AllowedHosts) {
						options.AllowedHosts.Add(allowedHost);
					}
				}
			});

			return services;
		}
	}

	/// <summary>
	/// 为 <see cref="IApplicationBuilder"/> 提供反向代理转发请求头中间件相关的扩展方法组
	/// </summary>
	/// <param name="app">当前应用构建器</param>
	extension(IApplicationBuilder app) {
		/// <summary>
		/// 依据已注册配置启用反向代理转发请求头处理中间件。
		/// </summary>
		/// <returns>当前应用构建器</returns>
		public IApplicationBuilder UseReverseProxyForwardedHeaders() =>
			app.ApplicationServices.GetRequiredService<ReverseProxyConfiguration>().Enabled
				// 转发请求头需要尽早处理，后续认证、签名校验、链接生成和日志写入才能看到外部请求上下文
				? app.UseForwardedHeaders()
				: app;
	}

	/// <summary>
	/// 从配置中读取反向代理转发请求头配置。
	/// </summary>
	/// <param name="configuration">应用配置</param>
	/// <returns>解析后的反向代理配置</returns>
	/// <exception cref="InvalidOperationException">当配置项存在但无法解析时</exception>
	/// <exception cref="FormatException">当配置项格式不正确时</exception>
	private static ReverseProxyConfiguration GetReverseProxyConfiguration(IConfiguration configuration) {
		var section = configuration.GetSection(ConfigurationSectionName);
		if (!section.GetValue("Enabled", false)) {
			return ReverseProxyConfiguration.Disabled;
		}

		var forwardLimit = section.GetValue("ForwardLimit", 1);
		if (forwardLimit < 1) {
			// ForwardLimit 必须大于等于 1，否则无法正确处理转发请求头
			throw new InvalidOperationException($"{ConfigurationSectionName}:ForwardLimit must be greater than or equal to 1.");
		}

		return new(
			true,
			forwardLimit,
			GetKnownNetworks(section),
			GetStringList(section, "AllowedHosts"));
	}

	/// <summary>
	/// 读取可信代理网段配置。
	/// </summary>
	/// <param name="section">反向代理配置节</param>
	/// <returns>可信代理网段列表</returns>
	/// <exception cref="FormatException">当网段配置不符合 CIDR 表示法时</exception>
	private static List<NetworkRange> GetKnownNetworks(IConfigurationSection section) {
		var knownNetworkValues = GetStringList(section, "KnownNetworks");
		var knownNetworks = new List<NetworkRange>(knownNetworkValues.Count);

		foreach (var knownNetworkValue in knownNetworkValues) {
			knownNetworks.Add(ParseKnownNetwork(knownNetworkValue));
		}

		return knownNetworks;
	}

	/// <summary>
	/// 将单个可信代理网段配置解析为 <see cref="NetworkRange"/>。
	/// </summary>
	/// <param name="value">CIDR 表示法的网段配置值</param>
	/// <returns>解析后的可信代理网段</returns>
	/// <exception cref="FormatException">当配置值无法解析为 CIDR 网段时</exception>
	private static NetworkRange ParseKnownNetwork(string value) =>
		NetworkRange.TryParse(value, out var network)
			? network
			: throw new FormatException($"{ConfigurationSectionName}:KnownNetworks value '{value}' must be a valid CIDR network.");

	/// <summary>
	/// 从配置节中读取字符串数组配置。
	/// </summary>
	/// <param name="section">配置节</param>
	/// <param name="key">配置键</param>
	/// <returns>已去除首尾空白的字符串列表</returns>
	/// <exception cref="FormatException">当配置数组中包含空字符串时</exception>
	private static List<string> GetStringList(IConfigurationSection section, string key) {
		var values = section.GetSection(key).Get<string[]>() ?? [];
		var list = new List<string>(values.Length);

		foreach (var value in values) {
			if (string.IsNullOrWhiteSpace(value)) {
				throw new FormatException($"{ConfigurationSectionName}:{key} cannot contain empty values.");
			}

			list.Add(value.Trim());
		}

		return list;
	}

	/// <summary>
	/// 已解析的反向代理转发请求头配置。
	/// </summary>
	/// <param name="Enabled">是否启用转发请求头处理中间件</param>
	/// <param name="ForwardLimit">最多处理多少层代理追加的请求头值</param>
	/// <param name="KnownNetworks">允许转发请求头生效的可信代理网段</param>
	/// <param name="AllowedHosts">允许通过 <c>X-Forwarded-Host</c> 写入的外部 Host 列表</param>
	private sealed record ReverseProxyConfiguration(
		bool Enabled,
		int ForwardLimit,
		IReadOnlyCollection<NetworkRange> KnownNetworks,
		IReadOnlyCollection<string> AllowedHosts) {
		public static ReverseProxyConfiguration Disabled => new(false, 1, [], []);
	}
}