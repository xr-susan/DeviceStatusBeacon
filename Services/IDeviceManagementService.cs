namespace DeviceStatusBeacon.Services;

/// <summary>
/// 为 Web 管理页面、Minimal API 和 CLI 管理命令提供共享设备管理能力的服务。
/// </summary>
/// <remarks>
/// 该服务聚焦于“设备管理”侧能力：
/// 统一处理设备创建、删除、重命名、密钥重置和基础设置更新。
/// 这样可以避免 CLI、页面和 API 各自维护一套相似但逐渐偏离的设备管理逻辑。
/// </remarks>
public interface IDeviceManagementService {
	/// <summary>
	/// 创建新设备。
	/// </summary>
	/// <param name="command">设备创建请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为设备创建结果</returns>
	Task<CreateDeviceCommandResult> CreateAsync(CreateDeviceCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 重置指定设备的操作密钥。
	/// </summary>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为密钥重置结果</returns>
	Task<ResetDeviceSecretKeyCommandResult> ResetSecretKeyAsync(Guid deviceId, CancellationToken cancellationToken = default);

	/// <summary>
	/// 重置指定设备的操作密钥。
	/// </summary>
	/// <param name="deviceName">设备名称</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为密钥重置结果</returns>
	Task<ResetDeviceSecretKeyCommandResult> ResetSecretKeyAsync(string deviceName, CancellationToken cancellationToken = default);

	/// <summary>
	/// 重命名指定设备。
	/// </summary>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="command">设备重命名请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task RenameAsync(Guid deviceId, RenameDeviceCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 重命名指定设备。
	/// </summary>
	/// <param name="oldDeviceName">旧设备名称</param>
	/// <param name="command">设备重命名请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task RenameAsync(string oldDeviceName, RenameDeviceCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定设备的显示名称。
	/// </summary>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="command">显示名称更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetDisplayNameAsync(Guid deviceId, SetDeviceDisplayNameCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定设备的显示名称。
	/// </summary>
	/// <param name="deviceName">设备名称</param>
	/// <param name="command">显示名称更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetDisplayNameAsync(string deviceName, SetDeviceDisplayNameCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定设备的启用状态。
	/// </summary>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="command">启用状态更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetEnabledAsync(Guid deviceId, SetDeviceEnabledCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定设备的启用状态。
	/// </summary>
	/// <param name="deviceName">设备名称</param>
	/// <param name="command">启用状态更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task SetEnabledAsync(string deviceName, SetDeviceEnabledCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 删除指定设备。
	/// </summary>
	/// <param name="deviceId">设备 ID</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task DeleteAsync(Guid deviceId, CancellationToken cancellationToken = default);

	/// <summary>
	/// 删除指定设备。
	/// </summary>
	/// <param name="deviceName">设备名称</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task DeleteAsync(string deviceName, CancellationToken cancellationToken = default);
}