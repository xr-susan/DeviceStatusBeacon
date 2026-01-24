namespace DeviceStatusBeacon.Models;

public enum AccountRole {
	/// <summary>
	/// 管理员账户，拥有所有权限
	/// </summary>
	Administrator,

	/// <summary>
	/// 全量查询账户，拥有查询所有设备关联数据的权限
	/// </summary>
	FullQuery,

	/// <summary>
	/// 有限设备查询账户，拥有查询指定设备关联数据的权限
	/// </summary>
	LimitedQuery
}

public class Account {
	/// <summary>
	/// 账户唯一标识符
	/// </summary>
	public Guid AccountId { get; set; }

	/// <summary>
	/// 账户唯一用户名
	/// </summary>
	public required string Username { get; set; }

	/// <summary>
	/// 账户操作密钥（以 AES IV 作为前缀）
	/// </summary>
	public required byte[] SecretKeyWithIV { get; set; }

	/// <summary>
	/// 账户角色
	/// </summary>
	public AccountRole Role { get; set; }

	/// <summary>
	/// 该账户有权限查询的设备列表，仅适用于 <see cref="Role"/> 为 <see cref="AccountRole.LimitedQuery"/> 的账户（由 EF Core 管理）
	/// </summary>
	public ICollection<Device> QueryableDevices { get; } = [];
}


public class Device {
	/// <summary>
	/// 设备唯一标识符
	/// </summary>
	public Guid DeviceId { get; set; }

	/// <summary>
	/// 设备唯一名称
	/// </summary>
	public required string DeviceName { get; set; }

	/// <summary>
	/// 设备操作密钥（以 AES IV 作为前缀）
	/// </summary>
	public required byte[] SecretKeyWithIV { get; set; }

	/// <summary>
	/// 设备显示名称（可选）
	/// </summary>
	public string? DisplayName { get; set; }


	/// <summary>
	/// 该设备最新上线日志的提交时间
	/// </summary>
	public DateTime LatestLogTime { get; set; }

	/// <summary>
	/// 该设备最新上线日志中报告的地址列表
	/// </summary>
	public IPAddress[] LatestReportedAddresses { get; set; } = [];

	/// <summary>
	/// 该设备最新上线日志中上报者的远程地址
	/// </summary>
	public IPAddress? LatestReporterRemoteAddress { get; set; }


	/// <summary>
	/// 设备是否启用
	/// </summary>
	public bool Enabled { get; set; } = true;


	/// <summary>
	/// 关联的上线日志列表（由 EF Core 管理）
	/// </summary>
	public ICollection<OnlineLog> OnlineLogs { get; } = [];

	/// <summary>
	/// 有权限查询该设备数据的账号列表，仅适用于 <see cref="Role"/> 为 <see cref="AccountRole.LimitedQuery"/> 的账户（由 EF Core 管理）
	/// </summary>
	public ICollection<Account> AuthorizedAccounts { get; } = [];
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
	public required IPAddress[] ReportedAddresses { get; set; }

	/// <summary>
	/// 该条日志中上报者的远程地址
	/// </summary>
	public IPAddress? ReporterRemoteAddress { get; set; }

	/// <summary>
	/// 该条日志的附加消息（可选）
	/// </summary>
	public string? Message { get; set; }


	/// <summary>
	/// 该条日志关联的设备对应的唯一标识符
	/// </summary>
	public Guid DeviceId { get; set; }

	/// <summary>
	/// 该条日志关联的设备实体（由 EF Core 管理）
	/// </summary>
	public Device Device { get; set; } = null!;
}