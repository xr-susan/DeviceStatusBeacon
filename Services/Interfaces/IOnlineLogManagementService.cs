using System.Net;

namespace DeviceStatusBeacon.Services.Interfaces;

/// <summary>
/// 在线日志管理服务。
/// </summary>
public interface IOnlineLogManagementService {
	/// <summary>
	/// 为指定设备创建一条在线日志。
	/// </summary>
	/// <param name="deviceId">路由中指定的目标设备 ID</param>
	/// <param name="command">在线日志创建请求</param>
	/// <param name="reporterRemoteAddress">请求来源的远程地址</param>
	/// <param name="submittedByUserId">代设备提交该条日志的用户 ID；如果当前主体就是设备，则为 null</param>
	/// <param name="authenticatedDevice">签名认证阶段缓存的设备实体；如果当前主体不是设备主体，则为 null</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务，任务结果为在线日志创建结果</returns>
	Task<CreateOnlineLogCommandResult> CreateAsync(
		Guid deviceId,
		CreateOnlineLogCommand command,
		IPAddress? reporterRemoteAddress,
		Guid? submittedByUserId,
		Device? authenticatedDevice,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新单条在线日志的附加消息。
	/// </summary>
	/// <param name="onlineLogId">在线日志 ID</param>
	/// <param name="command">在线日志消息更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task UpdateMessageAsync(long onlineLogId, UpdateOnlineLogMessageCommand command, CancellationToken cancellationToken = default);

	/// <summary>
	/// 删除单条在线日志。
	/// </summary>
	/// <param name="onlineLogId">在线日志 ID</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task DeleteAsync(long onlineLogId, CancellationToken cancellationToken = default);
}