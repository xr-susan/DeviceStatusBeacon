using System.Runtime.CompilerServices;

namespace DeviceStatusBeacon.Security;

public sealed class DataProtectorV1(IDataProtectionProvider dataProtectionProvider) : IDataProtectorV1 {
	private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(IDataProtectorV1.DataProtectionPurpose);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte[] ProtectKey(byte[] plainKey) => _protector.Protect(plainKey);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public byte[] UnprotectKey(byte[] protectedKey) => _protector.Unprotect(protectedKey);

	// public string ProtectString(string plainText) => _protector.Protect(plainText);
}