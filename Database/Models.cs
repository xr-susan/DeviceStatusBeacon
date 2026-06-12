using System.Net;

namespace DeviceStatusBeacon.Database;

/// <summary>
/// 主体角色枚举，定义了不同角色的权限范围
/// </summary>
/// <remarks>对应 int 值越大，权限范围越大，由于代码中涉及到了直接比较大小，此处保留显式数值，防止顺序意外被调换</remarks>
public enum PrincipalRole {
	/// <summary>
	/// 有限设备查询角色，拥有查询指定设备关联数据的权限
	/// </summary>
	LimitedQuery = 0,

	/// <summary>
	/// 全量查询角色，拥有查询所有设备关联数据的权限
	/// </summary>
	FullQuery = 1,

	/// <summary>
	/// 设备管理角色，拥有查询所有设备并管理设备的权限
	/// </summary>
	DeviceManager = 2,

	/// <summary>
	/// 管理员角色，拥有所有权限
	/// </summary>
	Administrator = 3
}


public class OnlineLog {
	/// <summary>
	/// 日志数字ID，自增字段，由数据库管理
	/// </summary>
	public long OnlineLogId { get; set; }

	/// <summary>
	/// 该条日志的提交时间
	/// </summary>
	public DateTime LogTime { get; set; }

	/// <summary>
	/// 该条日志中设备报告的地址列表
	/// </summary>
	/// <remarks>EF Core 会自动处理 <see cref="List{IPAddress}"/> 的值转换</remarks>
	public required List<IPAddress> ReportedAddresses { get; set; }

	/// <summary>
	/// 该条日志中上报者的远程地址
	/// </summary>
	/// <remarks>EF Core 会自动处理 <see cref="IPAddress"/> 的值转换</remarks>
	public IPAddress? ReporterRemoteAddress { get; set; }

	/// <summary>
	/// 该条日志的附加消息（可选）
	/// </summary>
	public string? Message { get; set; }

	/// <summary>
	/// 代设备提交该条日志的用户唯一标识符；如果该日志由设备自身直接提交，则为 null
	/// </summary>
	public Guid? SubmittedByUserId { get; set; }

	/// <summary>
	/// 代设备提交该条日志的用户实体；如果该日志由设备自身直接提交，则为 null
	/// </summary>
	/// <remarks>此实体由 EF Core 管理</remarks>
	public User? SubmittedByUser { get; set; }


	/// <summary>
	/// 该条日志关联的设备对应的唯一标识符
	/// </summary>
	public Guid DeviceId { get; set; }

	/// <summary>
	/// 该条日志关联的设备实体
	/// </summary>
	/// <remarks>此实体由 EF Core 管理</remarks>
	public Device Device { get; set; } = null!;
}