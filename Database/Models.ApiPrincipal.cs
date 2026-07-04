using System.Net;

namespace DeviceStatusBeacon.Database;

public interface IHasProtectedSecretKey {
	/// <summary>
	/// 经 ASP.NET Core 数据保护 API 保护后的操作密钥
	/// </summary>
	byte[] ProtectedSecretKey { get; }
}


public class ApiCredential : IHasProtectedSecretKey {
	/// <summary>
	/// API 凭据唯一标识符，用于签名鉴权
	/// </summary>
	public Guid ApiCredentialId { get; set; }

	/// <summary>
	/// API 凭据显示名称（可选）
	/// </summary>
	public string? DisplayName { get; set; }

	/// <summary>
	/// 所属用户唯一标识符
	/// </summary>
	public Guid UserId { get; set; }

	/// <summary>
	/// 所属用户
	/// </summary>
	/// <remarks>此实体由 EF Core 管理</remarks>
	public User User { get; set; } = null!;

	/// <summary>
	/// 经 ASP.NET Core 数据保护 API 保护后的 API 凭据操作密钥
	/// </summary>
	public required byte[] ProtectedSecretKey { get; set; }

	/// <summary>
	/// API 凭据角色
	/// </summary>
	public PrincipalRole Role { get; set; }

	/// <summary>
	/// API 凭据是否启用
	/// </summary>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// 该 API 凭据有权限查询的设备列表，仅适用于 <see cref="Role"/> 为 <see cref="PrincipalRole.LimitedQuery"/> 的 API 凭据
	/// </summary>
	/// <remarks>此集合由 EF Core 管理</remarks>
	public ICollection<Device> AuthorizedDevices { get; } = [];

	/// <summary>
	/// 该 API 凭据有权限查询的设备授权关系列表。
	/// </summary>
	/// <remarks>此集合由 EF Core 管理</remarks>
	public ICollection<ApiCredentialDevice> AuthorizedDeviceLinks { get; } = [];
}


public class ApiCredentialDevice {
	/// <summary>
	/// 被授权的 API 凭据 ID。
	/// </summary>
	public Guid AuthorizedApiCredentialsApiCredentialId { get; set; }

	/// <summary>
	/// 被授权的 API 凭据实体。
	/// </summary>
	/// <remarks>此实体由 EF Core 管理</remarks>
	public ApiCredential AuthorizedApiCredential { get; set; } = null!;

	/// <summary>
	/// 授权设备 ID。
	/// </summary>
	public Guid AuthorizedDevicesDeviceId { get; set; }

	/// <summary>
	/// 授权设备实体。
	/// </summary>
	/// <remarks>此实体由 EF Core 管理</remarks>
	public Device AuthorizedDevice { get; set; } = null!;
}


public class Device : IHasProtectedSecretKey {
	/// <summary>
	/// 设备唯一标识符
	/// </summary>
	public Guid DeviceId { get; set; }

	/// <summary>
	/// 设备唯一名称，用于人类管理
	/// </summary>
	public required string DeviceName { get; set; }

	/// <summary>
	/// 设备名称归一化结果，用于代码层面的唯一性判断和精确匹配
	/// </summary>
	public required string NormalizedDeviceName { get; set; }

	/// <summary>
	/// 经 ASP.NET Core 数据保护 API 保护后的设备操作密钥
	/// </summary>
	public required byte[] ProtectedSecretKey { get; set; }

	/// <summary>
	/// 设备显示名称（可选）
	/// </summary>
	public string? DisplayName { get; set; }


	/// <summary>
	/// 该设备最新上线日志的提交时间
	/// </summary>
	/// <remarks>此属性由 SQLite 触发器维护；当设备尚未有任何日志时，此值为 null</remarks>
	public DateTime? LatestLogTime { get; set; }

	/// <summary>
	/// 该设备最新上线日志中报告的地址列表
	/// </summary>
	/// <remarks>此属性由 SQLite 触发器维护；EF Core 会自动处理 <see cref="List{IPAddress}"/> 的值转换；当设备尚未有任何日志时，此值为 null</remarks>
	public List<IPAddress>? LatestReportedAddresses { get; set; }

	/// <summary>
	/// 该设备最新上线日志中上报者的远程地址
	/// </summary>
	/// <remarks>此属性由 SQLite 触发器维护；EF Core 会自动处理 <see cref="IPAddress"/> 的值转换；当设备尚未有任何日志时，此值为 null</remarks>
	public IPAddress? LatestReporterRemoteAddress { get; set; }


	/// <summary>
	/// 设备是否启用
	/// </summary>
	public bool Enabled { get; set; } = true;


	/// <summary>
	/// 关联的上线日志列表
	/// </summary>
	/// <remarks>此集合由 EF Core 管理</remarks>
	public ICollection<OnlineLog> OnlineLogs { get; } = [];

	/// <summary>
	/// 有权限查询该设备数据的 API 凭据列表，仅适用于 <see cref="Role"/> 为 <see cref="PrincipalRole.LimitedQuery"/> 的 API 凭据
	/// </summary>
	/// <remarks>此集合由 EF Core 管理</remarks>
	public ICollection<ApiCredential> AuthorizedApiCredentials { get; } = [];

	/// <summary>
	/// 有权限查询该设备数据的 API 凭据授权关系列表。
	/// </summary>
	/// <remarks>此集合由 EF Core 管理</remarks>
	public ICollection<ApiCredentialDevice> AuthorizedApiCredentialLinks { get; } = [];

	/// <summary>
	/// 有权限查询该设备数据的用户列表，仅适用于具有 <see cref="PrincipalRole.LimitedQuery"/> 角色的用户
	/// </summary>
	/// <remarks>此集合由 EF Core 管理</remarks>
	public ICollection<User> AuthorizedUsers { get; } = [];

	/// <summary>
	/// 有权限查询该设备数据的用户授权关系列表。
	/// </summary>
	/// <remarks>此集合由 EF Core 管理</remarks>
	public ICollection<DeviceUser> AuthorizedUserLinks { get; } = [];
}