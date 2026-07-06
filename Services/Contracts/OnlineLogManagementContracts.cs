using System.ComponentModel.DataAnnotations;
using System.Net;

namespace DeviceStatusBeacon.Services.Contracts;

/// <summary>
/// 在线日志创建请求。
/// </summary>
public sealed class CreateOnlineLogCommand : IValidatableObject {
	/// <summary>
	/// 本次上报的地址列表。
	/// </summary>
	[Required(ErrorMessage = "ReportedAddresses 不能为空。")]
	[MinLength(1, ErrorMessage = "ReportedAddresses 至少需要包含 1 个地址。")]
	[MaxLength(16, ErrorMessage = "ReportedAddresses 不能超过 16 个地址。")]
	public List<IPAddress> ReportedAddresses { get; init; } = [];

	/// <summary>
	/// 本次上报附带的附加消息。
	/// </summary>
	[StringLength(256, ErrorMessage = "Message 长度不能超过 256 个字符。")]
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
/// 在线日志消息更新请求。
/// </summary>
public sealed class UpdateOnlineLogMessageCommand : IValidatableObject {
	/// <summary>
	/// 新的附加消息；传入 null 表示清空消息。
	/// </summary>
	[StringLength(256, ErrorMessage = "Message 长度不能超过 256 个字符。")]
	public string? Message { get; init; }

	/// <inheritdoc/>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
		if (string.IsNullOrWhiteSpace(Message) && Message is not null) {
			yield return new("Message 不能为仅包含空白字符的字符串。", [nameof(Message)]);
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