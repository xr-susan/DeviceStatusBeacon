using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;

namespace DeviceStatusBeacon.Security;

/// <summary>
/// 鉴权方案枚举
/// </summary>
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


/// <summary>
/// 从 HTTP Authorization 头解析出的鉴权信息
/// </summary>
/// <param name="Scheme">鉴权方案</param>
/// <param name="Identity">用户身份</param>
/// <param name="Timestamp">时间戳</param>
/// <param name="Nonce">随机字符串</param>
/// <param name="SignatureBase64">签名的 Base64 字符串</param>
public sealed record AuthenticationHeaderV1(AuthenticationSchemeV1 Scheme, string Identity, long Timestamp, string Nonce, string SignatureBase64) {
	/// <summary>
	/// 尝试从 HTTP Authorization 头解析出 AuthenticationHeaderV1 实例
	/// </summary>
	/// <param name="authorizationHeaderValues">Authorization 请求头的值</param>
	/// <param name="result">解析结果（若解析成功，则为 AuthenticationHeaderV1 实例；否则为 null）</param>
	/// <returns>是否解析成功</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(StringValues? authorizationHeaderValues, [NotNullWhen(true)] out AuthenticationHeaderV1? result) {
		result = null;

		// 确保 authorizationHeaderValues 非 null 且有且仅有一个值
		return authorizationHeaderValues is [var input] && TryParse(input.AsSpan(), out result);
	}

	/// <summary>
	/// 尝试从 HTTP Authorization 头解析出 AuthenticationHeaderV1 实例
	/// </summary>
	/// <param name="authorizationHeaderValue">Authorization 请求头的值</param>
	/// <param name="result">解析结果（若解析成功，则为 AuthenticationHeaderV1 实例；否则为 null）</param>
	/// <returns>是否解析成功</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryParse(string? authorizationHeaderValue, [NotNullWhen(true)] out AuthenticationHeaderV1? result)
		=> TryParse(authorizationHeaderValue.AsSpan(), out result); // 利用 string? 的 AsSpan() 方法处理 null 情况

	/// <summary>
	/// 尝试从 HTTP Authorization 头解析出 AuthenticationHeaderV1 实例
	/// </summary>
	/// <param name="authorizationHeaderValue">Authorization 请求头的值</param>
	/// <param name="result">解析结果（若解析成功，则为 AuthenticationHeaderV1 实例；否则为 null）</param>
	/// <returns>是否解析成功</returns>
	public static bool TryParse(ReadOnlySpan<char> authorizationHeaderValue, [NotNullWhen(true)] out AuthenticationHeaderV1? result) {
		result = null;

		// Authorization: <Scheme> <Identity>:<Timestamp>:<Nonce>:<Signature>

		// 确保 authorizationHeaderValue 长度在允许范围内且非全空白
		if (authorizationHeaderValue.Length is < ISecurityServiceV1.MinAuthorizationHeaderValueLength or > ISecurityServiceV1.MaxAuthorizationHeaderValueLength
			|| authorizationHeaderValue.IsWhiteSpace()) {
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

		// 确保 identity 长度在允许范围内
		// 确保 nonce 长度在允许范围内
		// 确保 timestamp 可解析为 long
		// 确保 signatureBase64 解码后的长度合法
		if (identitySpan.Length is 0 or > ISecurityServiceV1.MaxIdentityLength
			|| nonceSpan.Length is < ISecurityServiceV1.MinNonceLength or > ISecurityServiceV1.MaxNonceLength
			|| !long.TryParse(timestampSpan, out var timestamp)
			|| ISecurityServiceV1.GetBase64DecodedLength(signatureBase64Span) != HMACSHA256.HashSizeInBytes) {
			return false;
		}

		result = new(scheme, identitySpan.ToString(), timestamp, nonceSpan.ToString(), signatureBase64Span.ToString());
		return true;
	}
}