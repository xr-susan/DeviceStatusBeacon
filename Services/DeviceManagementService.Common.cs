namespace DeviceStatusBeacon.Services;

public sealed partial class DeviceManagementService {
	/// <summary>
	/// 归一化设备名称。
	/// </summary>
	/// <param name="value">原始设备名称</param>
	/// <returns>归一化后的设备名称</returns>
	private string NormalizeDeviceName(string value) =>
		string.IsNullOrWhiteSpace(value)
			? throw new DeviceManagementCommandException(StatusCodes.Status422UnprocessableEntity, "设备名称不能为空")
			: lookupNormalizer.NormalizeName(value.Trim());

	/// <summary>
	/// 确保设备名称符合身份标识格式。
	/// </summary>
	/// <param name="deviceName">设备名称</param>
	/// <param name="message">格式错误时使用的错误消息</param>
	private static void EnsureValidDeviceName(string deviceName, string message) {
		if (!GeneratedRegex.IdentityRegex().IsMatch(deviceName)) {
			throw new DeviceManagementCommandException(StatusCodes.Status422UnprocessableEntity, message);
		}
	}

	/// <summary>
	/// 确保写入语句命中了目标设备。
	/// </summary>
	/// <param name="affectedCount">写入影响的行数</param>
	private static void EnsureDeviceFound(int affectedCount) {
		if (affectedCount == 0) {
			throw new DeviceManagementCommandException(StatusCodes.Status404NotFound, "未找到指定的设备");
		}
	}
}