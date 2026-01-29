namespace DeviceStatusBeacon.Security;

public class DataProtectorV1(IDataProtectionProvider dataProtectionProvider) : IDataProtectorV1 {
	protected readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(IDataProtectorV1.DataProtectionPurpose);

	public byte[] ProtectKey(byte[] plainKey) => _protector.Protect(plainKey);

	public byte[] UnprotectKey(byte[] protectedKey) => _protector.Unprotect(protectedKey);

	// public string ProtectString(string plainText) => _protector.Protect(plainText);
}