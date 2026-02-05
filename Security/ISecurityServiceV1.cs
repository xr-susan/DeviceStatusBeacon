using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace DeviceStatusBeacon.Security;

public interface ISecurityServiceV1 {
	/// <summary>
	/// 鉴权时传入的时间戳与当前时间的最大允许偏差（单位为秒）
	/// </summary>
	const long MaxAllowedTimestampDriftInSeconds = 300;

	/// <summary>
	/// 鉴权时传入的身份标识的最大长度
	/// </summary>
	const int MaxIdentityLength = 64;

	/// <summary>
	/// 鉴权时传入的身份标识的最小长度
	/// </summary>
	const int MinIdentityLength = 4;

	/// <summary>
	/// 鉴权时传入的随机字符串的最大长度
	/// </summary>
	const int MaxNonceLength = 64;

	/// <summary>
	/// 鉴权时传入的随机字符串的最小长度
	/// </summary>
	const int MinNonceLength = 28;

	/// <summary>
	/// 鉴权时 HTTP Authorization 头的最大允许长度
	/// </summary>
	/// <remarks>Scheme(&lt;=7) Identity(&lt;=<see cref="MaxIdentityLength"/>):Timestamp(&lt;=20):Nonce(&lt;=<see cref="MaxNonceLength"/>):Signature(44)</remarks>
	const int MaxAuthorizationHeaderValueLength = 7 + 1 + MaxIdentityLength + 1 + 20 + 1 + MaxNonceLength + 1 + 44;

	/// <summary>
	/// 鉴权时 HTTP Authorization 头的最小允许长度
	/// </summary>
	/// <remarks>Scheme(&gt;=6) Identity(&gt;=<see cref="MinIdentityLength"/>):Timestamp(&gt;=1):Nonce(&gt;=<see cref="MinNonceLength"/>):Signature(44)</remarks>
	const int MinAuthorizationHeaderValueLength = 6 + 1 + MinIdentityLength + 1 + 1 + 1 + MinNonceLength + 1 + 44;

	/// <summary>
	/// 安全的 UTF-8 编码器，遇到无效字节时会抛出异常，不带有 BOM
	/// </summary>
	static readonly UTF8Encoding SecureUtf8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	/// <summary>
	/// 基于给定实体的 SecretKey 和签名基础信息计算签名
	/// </summary>
	/// <param name="entity">用于获取 SecretKey 的实体</param>
	/// <param name="signatureBasis">用于计算签名的基础信息</param>
	/// <returns>计算得到的签名</returns>
	ReadOnlySpan<byte> ComputeSignature(IHasProtectedSecretKey entity, SignatureBasisV1 signatureBasis);

	/// <summary>
	/// 基于给定实体的 SecretKey 和签名基础信息验证签名
	/// </summary>
	/// <param name="entity">用于获取 SecretKey 的实体</param>
	/// <param name="signatureBasis">用于验证签名的基础信息</param>
	/// <param name="signature">待验证的签名</param>
	/// <returns>验证结果</returns>
	bool VerifySignature(IHasProtectedSecretKey entity, SignatureBasisV1 signatureBasis, ReadOnlySpan<byte> signature);

	/// <summary>
	/// 基于给定实体的 SecretKey 和签名基础信息验证签名
	/// </summary>
	/// <param name="entity">用于获取 SecretKey 的实体</param>
	/// <param name="signatureBasis">用于验证签名的基础信息</param>
	/// <param name="signatureBase64">待验证的签名的 Base64 字符串</param>
	/// <returns>验证结果</returns>
	bool VerifySignature(IHasProtectedSecretKey entity, SignatureBasisV1 signatureBasis, string signatureBase64);

	/// <summary>
	/// 生成指定长度的随机字节数组
	/// </summary>
	/// <param name="keySizeInBytes">字节数组的长度（单位为字节）</param>
	/// <returns>生成的随机字节数组</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static sealed byte[] GenerateRandomBytes(int keySizeInBytes = HMACSHA256.HashSizeInBytes) => RandomNumberGenerator.GetBytes(keySizeInBytes);

	/// <summary>
	/// 使用随机数生成器填充指定的字节缓冲区
	/// </summary>
	/// <param name="buffer">需要被填充的字节缓冲区</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static sealed void FillRandomBytes(Span<byte> buffer) => RandomNumberGenerator.Fill(buffer);

	/// <summary>
	/// 检查时间戳是否在允许的偏差范围内
	/// </summary>
	/// <param name="timestamp">客户端传入的时间戳</param>
	/// <returns>若时间戳在允许的偏差范围内则返回 true，否则返回 false</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static sealed bool IsTimestampWithinAllowedDrift(long timestamp) {
		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		return Math.Abs(now - timestamp) <= MaxAllowedTimestampDriftInSeconds;
	}

	/// <summary>
	/// 计算 Base64 编码字符串解码后的长度
	/// </summary>
	/// <remarks>此方法不会实际执行解码操作，也不会确保输入字符串是有效的 Base64 编码字符串，仅用于计算解码后的长度</remarks>
	/// <param name="base64String">需要计算解码长度的 Base64 编码字符串</param>
	/// <returns>如果输入字符串为空，则返回 0；如果输入字符串长度不合法，则返回 -1；否则返回解码后的长度</returns>
	static int GetBase64DecodedLength(ReadOnlySpan<char> base64String) {
		if (base64String.IsEmpty) {
			return 0;
		}

		var length = base64String.Length;

		// Base64 字符串长度必须是 4 的倍数
		if (length % 4 != 0) {
			return -1;
		}

		// 计算填充字符的数量
		// 前面的逻辑已经确保 length >= 4，无需担心索引越界
		var padding = base64String[^2..] switch {
			"==" => 2,
			[.., '='] => 1,
			_ => 0,
		};

		return (length / 4 * 3) - padding;
	}
}