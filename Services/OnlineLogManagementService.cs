using System.Net;
using Microsoft.Data.Sqlite;

namespace DeviceStatusBeacon.Services;

/// <summary>
/// 在线日志管理服务。
/// </summary>
public sealed class OnlineLogManagementService(DeviceStatusBeaconContext dbContext) : IOnlineLogManagementService {
	/// <inheritdoc/>
	public async Task<CreateOnlineLogCommandResult> CreateAsync(
		Guid deviceId,
		CreateOnlineLogCommand command,
		IPAddress? reporterRemoteAddress,
		Guid? submittedByUserId,
		Device? authenticatedDevice,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new OnlineLogManagementException(StatusCodes.Status422UnprocessableEntity, message));

		if (authenticatedDevice is not null) {
			if (authenticatedDevice.DeviceId != deviceId) {
				// 如果当前主体就是设备本身，则目标设备必须与该设备完全一致
				throw new OnlineLogManagementException(StatusCodes.Status403Forbidden, "设备主体只能向自身写入日志。");
			}
		} else {
			// 如果当前主体不是设备，则必须有 submittedByUserId 存在
			if (submittedByUserId is null) {
				throw new OnlineLogManagementException(StatusCodes.Status403Forbidden, "未能识别提交日志的主体。");
			}

			// 校验目标设备是否存在
			if (!await dbContext.Devices.AsNoTracking().AnyAsync(d => d.DeviceId == deviceId, cancellationToken)) {
				throw new OnlineLogManagementException(StatusCodes.Status404NotFound, "未找到指定的目标设备。");
			}
		}

		// 构造日志实体并直接写入数据库；
		// 当前日志时间统一以后端确认写入时间为准，请求方不参与指定
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
			throw new OnlineLogManagementException(StatusCodes.Status404NotFound, "目标设备或提交用户已不存在。");
		}

		return new(
			newLog.OnlineLogId,
			newLog.DeviceId,
			newLog.LogTime);
	}
}