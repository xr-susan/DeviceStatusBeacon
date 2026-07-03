using System.Net;
using Microsoft.Data.Sqlite;

namespace DeviceStatusBeacon.Services;

/// <summary>
/// 在线日志命令服务。
/// </summary>
/// <remarks>
/// 该服务把在线日志相关的写入规则收敛到同一个地方：
/// 设备主体默认只能向自身写入；
/// 具备跨设备写入能力的主体可以代设备写入；
/// 非法目标设备和越权写入都在这里统一转成明确的失败结果。
/// </remarks>
public sealed class OnlineLogCommandService(DeviceStatusBeaconContext dbContext) : IOnlineLogCommandService {
	/// <inheritdoc/>
	public async Task<CreateOnlineLogCommandResult> CreateAsync(
		Guid deviceId,
		CreateOnlineLogCommand command,
		IPAddress? reporterRemoteAddress,
		Guid? submittedByUserId,
		Device? authenticatedDevice,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);

		if (authenticatedDevice is not null) {
			if (authenticatedDevice.DeviceId != deviceId) {
				// 如果当前主体就是设备本身，则目标设备必须与该设备完全一致
				throw new OnlineLogCommandException(StatusCodes.Status403Forbidden, "设备主体只能向自身写入日志。");
			}
		} else {
			// 如果当前主体不是设备，则必须有 submittedByUserId 存在
			if (submittedByUserId is null) {
				throw new OnlineLogCommandException(StatusCodes.Status403Forbidden, "未能识别提交日志的主体。");
			}

			// 校验目标设备是否存在
			if (!await dbContext.Devices.AsNoTracking().AnyAsync(d => d.DeviceId == deviceId, cancellationToken)) {
				throw new OnlineLogCommandException(StatusCodes.Status404NotFound, "未找到指定的目标设备。");
			}
		}

		// 构造日志实体并直接写入数据库；
		// 当前日志时间统一以后端确认写入时间为准，请求方不参与指定。
		var newLog = new OnlineLog {
			DeviceId = deviceId,
			LogTime = DateTime.UtcNow,
			ReportedAddresses = command.ReportedAddresses,
			ReporterRemoteAddress = reporterRemoteAddress,
			SubmittedByUserId = submittedByUserId,
			Message = command.Message?.Trim() // Message 可能为 null，但不会是空白字符串，直接 ?.Trim() 即可
		};

		try {
			dbContext.OnlineLogs.Add(newLog);
			await dbContext.SaveChangesAsync(cancellationToken);
		} catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 787 }) {
			throw new OnlineLogCommandException(StatusCodes.Status404NotFound, "目标设备或提交用户已不存在。");
		}

		return new(
			newLog.OnlineLogId,
			newLog.DeviceId,
			newLog.LogTime);
	}
}