using System.ComponentModel.DataAnnotations;

namespace DeviceStatusBeacon.Services;

/// <summary>
/// 设备创建请求。
/// </summary>
public sealed class CreateDeviceCommand {
	/// <summary>
	/// 设备名称。
	/// </summary>
	[Required]
	public required string DeviceName { get; init; }

	/// <summary>
	/// 设备显示名称。
	/// </summary>
	public string? DisplayName { get; init; }
}

/// <summary>
/// 设备重命名请求。
/// </summary>
public sealed class RenameDeviceCommand {
	/// <summary>
	/// 新设备名称。
	/// </summary>
	[Required]
	public required string NewDeviceName { get; init; }
}

/// <summary>
/// 设备显示名称更新请求。
/// </summary>
public sealed class SetDeviceDisplayNameCommand {
	/// <summary>
	/// 新显示名称。
	/// </summary>
	/// <remarks>设为 null 表示清空显示名称。</remarks>
	public required string? DisplayName { get; init; }
}

/// <summary>
/// 设备启用状态更新请求。
/// </summary>
public sealed class SetDeviceEnabledCommand {
	/// <summary>
	/// 是否启用设备。
	/// </summary>
	[Required]
	public required bool Enabled { get; init; }
}

/// <summary>
/// 设备创建结果。
/// </summary>
/// <param name="DeviceId">新设备 ID</param>
/// <param name="DeviceName">设备名称</param>
/// <param name="DisplayName">设备显示名称</param>
/// <param name="SecretKey">设备的操作密钥，只在创建结果中返回一次</param>
public sealed record CreateDeviceCommandResult(
	Guid DeviceId,
	string DeviceName,
	string? DisplayName,
	string SecretKey
);

/// <summary>
/// 设备密钥重置结果。
/// </summary>
/// <param name="SecretKey">设备的新操作密钥，只在重置结果中返回一次</param>
public sealed record ResetDeviceSecretKeyCommandResult(
	string SecretKey
);

/// <summary>
/// 表示设备管理命令执行过程中出现的业务失败。
/// </summary>
public sealed class DeviceManagementCommandException(int statusCode, string message) : Exception(message) {
	/// <summary>
	/// 对应的 HTTP 状态码。
	/// </summary>
	public int StatusCode { get; } = statusCode;
}