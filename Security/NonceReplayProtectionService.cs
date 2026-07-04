namespace DeviceStatusBeacon.Security;

/// <summary>
/// 为签名式鉴权提供 nonce 防重放保护的服务
/// </summary>
public interface INonceReplayProtectionService {
	/// <summary>
	/// nonce 清理周期
	/// </summary>
	protected internal static readonly TimeSpan NonceCleanupInterval = TimeSpan.FromSeconds(60);

	/// <summary>
	/// 尝试预留当前鉴权头中的 nonce，以防止后续重放
	/// </summary>
	/// <param name="authHeader">已经通过格式校验与时间戳校验的鉴权头</param>
	/// <returns>如果 nonce 预留成功则返回 true；如果 nonce 已被占用或当前请求已失效则返回 false</returns>
	bool TryReserve(AuthenticationHeaderV1 authHeader);

	/// <summary>
	/// 清理当前已经过期的 nonce 键
	/// </summary>
	void CleanupExpiredNonces();
}

/// <summary>
/// 基于进程内存字典的 nonce 防重放服务
/// </summary>
public sealed class NonceReplayProtectionService : INonceReplayProtectionService {
	/// <summary>
	/// 用于保护 nonce 字典读写的一把锁
	/// </summary>
	private readonly Lock _lock = new();

	/// <summary>
	/// 当前已经预留的 nonce 键及其过期时间戳（单位为秒）
	/// </summary>
	private readonly Dictionary<string, long> reservedNonceExpirations = new(StringComparer.Ordinal);

	/// <inheritdoc/>
	public bool TryReserve(AuthenticationHeaderV1 authHeader) {
		var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var absoluteExpiration = authHeader.Timestamp + ISecurityServiceV1.MaxAllowedTimestampDriftInSeconds;

		// 理论上调用方已经完成了时间戳校验；这里仍补一层兜底，避免把已失效请求写入防重放结构
		if (absoluteExpiration <= currentTimestamp) {
			return false;
		}

		// 以鉴权方案、主体标识和 nonce 组合出唯一键，避免不同主体之间互相影响
		// GUID 之间冲突的概率极低，此处不区分 Device 和 ApiCredential 这两类主体标识
		var nonceCacheKey = $"{authHeader.Scheme}:{authHeader.Identity:D}:{authHeader.Nonce}";

		lock (_lock) {
			// nonce 的主索引只保留一份字典：
			// 如果键不存在，则直接写入；
			// 如果键已存在但已过期，则原位刷新；
			// 如果键已存在且尚未过期，则视为重放
			if (reservedNonceExpirations.TryGetValue(nonceCacheKey, out var existingExpiration)
				&& existingExpiration > currentTimestamp) {
				return false;
			}

			reservedNonceExpirations[nonceCacheKey] = absoluteExpiration;
			return true;
		}
	}

	/// <inheritdoc/>
	public void CleanupExpiredNonces() {
		List<string>? expiredNonceKeys = null;
		var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

		lock (_lock) {
			// 遍历字典找出所有过期的 nonce 键
			// 不在遍历过程中直接修改字典，避免枚举器失效
			foreach (var pair in reservedNonceExpirations) {
				if (pair.Value <= currentTimestamp) {
					expiredNonceKeys ??= [];
					expiredNonceKeys.Add(pair.Key);
				}
			}

			// 如果没有过期的 nonce 键，则直接返回；否则逐个从字典中移除这些键
			if (expiredNonceKeys is { Count: > 0 }) {
				foreach (var nonceKey in expiredNonceKeys) {
					reservedNonceExpirations.Remove(nonceKey);
				}
			}
		}
	}
}