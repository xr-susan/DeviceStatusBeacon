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

		// 确保 authorizationHeaderValues 有且仅有一个值且非 null
		return authorizationHeaderValues is [var input] && input is not null && TryParse(input.AsSpan(), out result);
	}

	public static bool TryParse(string? authorizationHeaderValue, out AuthenticationHeaderV1? result) {
		result = null;

		// 确保 authorizationHeaderValue 非 null
		return authorizationHeaderValue is not null && TryParse(authorizationHeaderValue.AsSpan(), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> authorizationHeaderValue, out AuthenticationHeaderV1? result) {
		result = null;

		// Authorization: <Scheme> <Identity>:<Timestamp>:<Nonce>:<Signature>

		// 确保 authorizationHeaderValue 非0长度或全空白
		if (authorizationHeaderValue.IsWhiteSpace()) {
			return false;
		}

		// 切分 authorizationHeaderValue 为两部分，此处的 3 是有意为之
		Span<Range> headerPartsRange = stackalloc Range[3];

		// 确保 authorizationHeaderValue 中有且仅有一个空格
		if (authorizationHeaderValue.Split(headerPartsRange, ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			!= 2) {
			return false;
		}

		// 解析 Scheme，确保是存在且有效的 Scheme
		if (!Enum.TryParse(authorizationHeaderValue[headerPartsRange[0]], out AuthenticationSchemeV1 scheme) || scheme == AuthenticationSchemeV1.Unknown) {
			return false;
		}

		// 切分 dataPart 为四部分，此处的 5 是有意为之
		var dataPart = authorizationHeaderValue[headerPartsRange[1]];
		Span<Range> dataPartsRange = stackalloc Range[5];

		// 确保 dataPartsRange 中有且仅有三个冒号
		if (dataPart.Split(dataPartsRange, ':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			!= 4) {
			return false;
		}

		var identitySpan = dataPart[dataPartsRange[0]];
		var timestampSpan = dataPart[dataPartsRange[1]];
		var nonceSpan = dataPart[dataPartsRange[2]];
		var signatureBase64Span = dataPart[dataPartsRange[3]];

		// 确保 nonce 长度在允许范围内
		// 确保 timestamp 可解析为 long
		// 确保 signatureBase64 解码后的长度合法
		if (nonceSpan.Length is < ISecurityServiceV1.MinNonceLength or > ISecurityServiceV1.MaxNonceLength
			|| !long.TryParse(timestampSpan, out var timestamp)
			|| ISecurityServiceV1.GetBase64DecodedLength(signatureBase64Span) != HMACSHA256.HashSizeInBytes) {
			return false;
		}

		result = new(scheme, identitySpan.ToString(), timestamp, nonceSpan.ToString(), signatureBase64Span.ToString());
		return true;
	}
}