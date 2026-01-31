using System.Runtime.CompilerServices;

namespace DeviceStatusBeacon.Security;

public sealed class SecurityServiceV1(IDataProtectorV1 dataProtector) : ISecurityServiceV1 {
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<byte> ComputeSignature(IHasProtectedSecretKey entity, SignatureBasisV1 signatureBasis) =>
		signatureBasis.ComputeSignature(dataProtector.UnprotectKeyFromEntity(entity));

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool VerifySignature(IHasProtectedSecretKey entity, SignatureBasisV1 signatureBasis, ReadOnlySpan<byte> signature) =>
		signatureBasis.VerifySignature(dataProtector.UnprotectKeyFromEntity(entity), signature);

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool VerifySignature(IHasProtectedSecretKey entity, SignatureBasisV1 signatureBasis, string signatureBase64) =>
		signatureBasis.VerifySignature(dataProtector.UnprotectKeyFromEntity(entity), signatureBase64);
}