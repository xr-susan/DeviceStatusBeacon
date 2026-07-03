using Microsoft.AspNetCore.Identity;

namespace DeviceStatusBeacon.Services;

/// <summary>
/// 设备管理服务。
/// </summary>
/// <remarks>
/// 该服务把设备相关的写入规则收敛到同一个地方：
/// CLI、Minimal API 和后续管理页面都通过这里创建、修改和删除设备；
/// 设备名称格式、唯一性冲突和目标设备存在性也在这里统一处理。
/// </remarks>
public sealed partial class DeviceManagementService(
	DeviceStatusBeaconContext dbContext,
	ILookupNormalizer lookupNormalizer,
	IDataProtectorV1 dataProtector) : IDeviceManagementService {
	/// <summary>
	/// 删除设备前要求的无新日志时间窗口。
	/// </summary>
	private static readonly TimeSpan DeleteRecentLogBlockWindow = TimeSpan.FromDays(7);

	/// <summary>
	/// 设备管理目标。
	/// </summary>
	/// <param name="DeviceId">设备 ID</param>
	/// <param name="Enabled">是否启用</param>
	private sealed record DeviceManagementTarget(
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
			?? throw new DeviceManagementCommandException(StatusCodes.Status404NotFound, "未找到指定的设备");

	/// <summary>
	/// 确保设备名称符合身份标识格式。
	/// </summary>
	/// <param name="deviceName">设备名称</param>
	/// <param name="message">格式错误时使用的错误消息</param>
	private static void EnsureValidDeviceName(string deviceName, string message) {
		if (!IdentityNameRules.IsValid(deviceName)) {
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