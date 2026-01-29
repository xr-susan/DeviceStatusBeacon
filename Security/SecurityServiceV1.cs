using System.Security.Cryptography;

namespace DeviceStatusBeacon.Security;

public class SecurityServiceV1(IDataProtectorV1 dataProtector) : ISecurityServiceV1 {
	/// <inheritdoc/>
	public ReadOnlySpan<byte> ComputeSignature(IHasProtectedSecretKey entity, SignatureBasis signatureBasis) =>
		signatureBasis.ComputeSignature(dataProtector.UnprotectKeyFromEntity(entity));

	/// <inheritdoc/>
	public bool VerifySignature(IHasProtectedSecretKey entity, SignatureBasis signatureBasis, ReadOnlySpan<byte> signature) =>
		signatureBasis.VerifySignature(dataProtector.UnprotectKeyFromEntity(entity), signature);

	/// <inheritdoc/>
	public bool VerifySignature(IHasProtectedSecretKey entity, SignatureBasis signatureBasis, string signatureBase64) =>
		signatureBasis.VerifySignature(dataProtector.UnprotectKeyFromEntity(entity), signatureBase64);
}

/// <summary>
/// 用于计算签名的基础信息
/// </summary>
/// <param name="RequestMethod">用于计算签名的请求方法</param>
/// <param name="RequestPathAndQuery">用于计算签名的请求路径和查询字符串</param>
/// <param name="Timestamp">用于计算签名的时间戳</param>
/// <param name="Nonce">用于计算签名的随机字符串</param>
public record SignatureBasis(string RequestMethod, string RequestPathAndQuery, long Timestamp, string Nonce) {
	/// <summary>
	/// 返回用于计算签名的字符串
	/// </summary>
	/// <returns>用于计算签名的字符串</returns>
	public override string ToString() => $"{RequestMethod} {RequestPathAndQuery}\n{Timestamp}\n{Nonce}";

	/// <summary>
	/// 返回用于计算签名的 UTF-8 字节序列
	/// </summary>
	/// <returns>用于计算签名的 UTF-8 字节序列</returns>
	public ReadOnlySpan<byte> GetUtf8Bytes() => ISecurityServiceV1.SecureUtf8Encoding.GetBytes(ToString());

	/// <summary>
	/// 计算签名
	/// </summary>
	/// <param name="secretKey">用于计算签名的 SecretKey</param>
	/// <returns>计算得到的签名</returns>
	public ReadOnlySpan<byte> ComputeSignature(ReadOnlySpan<byte> secretKey) =>
		HMACSHA256.HashData(secretKey, GetUtf8Bytes());

	/// <summary>
	/// 验证签名是否正确
	/// </summary>
	/// <param name="secretKey">用于计算签名的 SecretKey</param>
	/// <param name="signature">待验证的签名</param>
	/// <returns>验证结果</returns>
	public bool VerifySignature(ReadOnlySpan<byte> secretKey, ReadOnlySpan<byte> signature) {
		// 计算预期的签名并进行固定时间比较
		var expectedSignature = ComputeSignature(secretKey);
		return CryptographicOperations.FixedTimeEquals(expectedSignature, signature);
	}

	/// <summary>
	/// 验证签名是否正确
	/// </summary>
	/// <param name="secretKey">用于计算签名的 SecretKey</param>
	/// <param name="signatureBase64">待验证的签名的 Base64 字符串</param>
	/// <returns>验证结果</returns>
	public bool VerifySignature(ReadOnlySpan<byte> secretKey, string signatureBase64) {
		// 检查签名长度是否正确
		if (ISecurityServiceV1.GetBase64DecodedLength(signatureBase64) is not HMACSHA256.HashSizeInBytes) {
			return false;
		}

		Span<byte> decodedSignature = stackalloc byte[HMACSHA256.HashSizeInBytes];

		// 解码 Base64 字符串并验证签名
		return Convert.TryFromBase64String(signatureBase64, decodedSignature, out var bytesWritten)
			&& VerifySignature(secretKey, decodedSignature[..bytesWritten]);
	}

	/// <summary>
	/// 通过 HTTP 请求创建用于计算签名的基础信息
	/// </summary>
	/// <param name="request">用于获取请求信息的 HTTP 请求</param>
	/// <param name="timestamp">用于计算签名的时间戳</param>
	/// <param name="nonce">用于计算签名的随机字符串</param>
	/// <returns>创建的签名基础信息</returns>
	public static SignatureBasis FromHttpRequest(HttpRequest request, long timestamp, string nonce) =>
		new(request.Method, $"{request.Path}{request.QueryString}", timestamp, nonce);
}