using Microsoft.AspNetCore.Identity;

namespace DeviceStatusBeacon.Services;

/// <summary>
/// 设备管理服务。
/// </summary>
public sealed partial class DeviceAdministrationService(
	DeviceStatusBeaconContext dbContext,
	ILookupNormalizer lookupNormalizer,
	IDataProtectorV1 dataProtector) : IDeviceAdministrationService {
	/// <summary>
	/// 设备管理目标。
	/// </summary>
	/// <param name="DeviceId">设备 ID</param>
	/// <param name="Enabled">是否启用</param>
	private sealed record DeviceAdministrationTarget(
		Guid DeviceId,
		bool Enabled
	);

	/// <summary>
	/// 创建设备名称查找条件。
	/// </summary>
	/// <param name="value">原始设备名称</param>
	/// <returns>设备名称查找条件</returns>
	private IdentityNameLookup CreateDeviceNameLookup(string value) =>
		IdentityNameLookup.TryCreate(value, lookupNormalizer)
			?? throw new DeviceAdministrationException(StatusCodes.Status404NotFound, "未找到指定的设备");

	/// <summary>
	/// 确保设备名称符合身份标识格式。
	/// </summary>
	/// <param name="deviceName">设备名称</param>
	/// <param name="message">格式错误时使用的错误消息</param>
	private static void EnsureValidDeviceName(string deviceName, string message) {
		if (!IdentityNameRules.IsValid(deviceName)) {
			throw new DeviceAdministrationException(StatusCodes.Status422UnprocessableEntity, message);
		}
	}

	/// <summary>
	/// 确保写入语句命中了目标设备。
	/// </summary>
	/// <param name="affectedCount">写入影响的行数</param>
	private static void EnsureDeviceFound(int affectedCount) {
		if (affectedCount == 0) {
			throw new DeviceAdministrationException(StatusCodes.Status404NotFound, "未找到指定的设备");
		}
	}
}