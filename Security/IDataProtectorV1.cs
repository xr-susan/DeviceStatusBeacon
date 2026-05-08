using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.DataProtection;

namespace DeviceStatusBeacon.Security;

public interface IDataProtectorV1 {
	/// <summary>
	/// 传递给 <see cref="IDataProtectionProvider.CreateProtector(string)"/> 的目的字符串
	/// </summary>
	const string DataProtectionPurpose = "V1";

	/// <summary>
	/// 通过数据保护 API 保护密钥
	/// </summary>
	/// <param name="plainKey">原始密钥</param>
	/// <returns>受保护的密钥</returns>
	byte[] ProtectKey(byte[] plainKey);

	/// <summary>
	/// 通过数据保护 API 解除保护密钥
	/// </summary>
	/// <param name="protectedKey">受保护的密钥</param>
	/// <returns>原始密钥</returns>
	byte[] UnprotectKey(byte[] protectedKey);

	/// <summary>
	/// 通过数据保护 API 解除保护从实体中获取的受保护的密钥
	/// </summary>
	/// <param name="entity">对应的实体</param>
	/// <returns>实体的原始密钥</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	sealed ReadOnlySpan<byte> UnprotectKeyFromEntity(IHasProtectedSecretKey entity) =>
		UnprotectKey(entity.ProtectedSecretKey);
}