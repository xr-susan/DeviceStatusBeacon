using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;

namespace DeviceStatusBeacon.Security;

public enum AuthenticationSchemeV1 {
	/// <summary>
	/// 未知鉴权方案
	/// </summary>
	Unknown,

	/// <summary>
	/// 用户账号鉴权方案
	/// </summary>
	Account,

	/// <summary>
	/// 设备鉴权方案
	/// </summary>
	Device
}


public record AuthenticationHeaderV1(AuthenticationSchemeV1 Scheme, string Identity, long Timestamp, string Nonce, string SignatureBase64) {
	public static bool TryParse(StringValues? authorizationHeaderValues, out AuthenticationHeaderV1? result) {
		result = null;

		// 确保 authorizationHeaderValues 有且仅有一个值
		return authorizationHeaderValues is { Count: 1 } && TryParse(authorizationHeaderValues?[0], out result);
	}

	public static bool TryParse(string? authorizationHeaderValue, out AuthenticationHeaderV1? result) {
		result = null;

		// Authorization: <Scheme> <Identity>:<Timestamp>:<Nonce>:<Signature>

		// 确保 authorizationHeaderValue 中有且仅有一个空格
		var headerParts = authorizationHeaderValue?.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); // 此处的 3 是有意为之
		if (headerParts is not { Length: 2 }) {
			return false;
		}

		// 解析 Scheme，确保是存在且有效的 Scheme
		if (!Enum.TryParse(headerParts[0], out AuthenticationSchemeV1 scheme) || scheme == AuthenticationSchemeV1.Unknown) {
			return false;
		}

		// 确保 valueParts 中有且仅有三个冒号
		var valueParts = headerParts[1].Split(':', 5, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); // 此处的 5 是有意为之
		if (valueParts is not { Length: 4 }) {
			return false;
		}

		// 确保 timestamp 可解析为 long
		if (!long.TryParse(valueParts[1], out var timestamp)) {
			return false;
		}

		// 确保 nonce 长度在允许范围内
		if (valueParts[2].Length is < ISecurityServiceV1.MinNonceLength or > ISecurityServiceV1.MaxNonceLength) {
			return false;
		}

		// 确保 signatureBase64 解码后的长度合法
		if (ISecurityServiceV1.GetBase64DecodedLength(valueParts[3]) != HMACSHA256.HashSizeInBytes) {
			return false;
		}

		result = new(
			scheme,
			valueParts[0],
			timestamp,
			valueParts[2],
			valueParts[3]
		);
		return true;
	}
}