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
}