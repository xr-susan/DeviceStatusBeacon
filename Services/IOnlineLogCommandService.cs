using System.ComponentModel.DataAnnotations;
using System.Net;

namespace DeviceStatusBeacon.Services;

/// <summary>
/// 为在线日志写入入口提供共享命令能力的服务。
/// </summary>
/// <remarks>
/// 该服务聚焦于“写入”侧能力：
/// 统一处理目标设备解析、写入边界校验和日志落库。
/// </remarks>
public interface IOnlineLogCommandService {
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
}

/// <summary>
/// 在线日志创建请求。
/// </summary>
public sealed class CreateOnlineLogCommand : IValidatableObject {
	/// <summary>
	/// 本次上报的地址列表。
	/// </summary>
	[Required]
	[MinLength(1)]
	[MaxLength(16)]
	public List<IPAddress> ReportedAddresses { get; init; } = [];

	/// <summary>
	/// 本次上报附带的附加消息。
	/// </summary>
	[StringLength(256)]
	public string? Message { get; init; }

	/// <inheritdoc/>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
		if (string.IsNullOrWhiteSpace(Message) && Message is not null) {
			yield return new("Message 不能为仅包含空白字符的字符串。", [nameof(Message)]);
			yield break;
		}

		// IP 地址列表中拒绝 null 元素和重复项，避免后续写入层接收语义含糊的地址集合
		HashSet<IPAddress>? uniqueAddresses = null;
		for (var i = 0; i < ReportedAddresses.Count; i++) {
			var address = ReportedAddresses[i];
			if (address is null) {
				yield return new("ReportedAddresses 中的地址不能为 null。", [nameof(ReportedAddresses)]);
				yield break;
			}

			uniqueAddresses ??= [];
			if (!uniqueAddresses.Add(address)) {
				yield return new("ReportedAddresses 中不允许出现重复地址。", [nameof(ReportedAddresses)]);
				yield break;
			}
		}
	}
}

/// <summary>
/// 在线日志创建结果。
/// </summary>
/// <param name="OnlineLogId">新日志 ID</param>
/// <param name="DeviceId">日志关联的目标设备 ID</param>
/// <param name="LogTime">最终写入数据库的日志时间</param>
public sealed record CreateOnlineLogCommandResult(
	long OnlineLogId,
	Guid DeviceId,
	DateTime LogTime
);

/// <summary>
/// 表示在线日志命令执行过程中出现的业务失败。
/// </summary>
public sealed class OnlineLogCommandException(int statusCode, string message)
	: ServiceCommandException(statusCode, GetProblemTitle(statusCode), message) {
	/// <summary>
	/// 获取在线日志创建失败对应的错误标题。
	/// </summary>
	/// <param name="statusCode">HTTP 状态码</param>
	/// <returns>用于 ProblemDetails 的错误标题</returns>
	private static string GetProblemTitle(int statusCode) => statusCode switch {
		StatusCodes.Status400BadRequest => "设备日志请求无效",
		StatusCodes.Status403Forbidden => "不允许写入日志",
		StatusCodes.Status404NotFound => "目标设备不存在",
		_ => "日志写入失败"
	};
}