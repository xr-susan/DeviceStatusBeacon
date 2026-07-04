using Microsoft.AspNetCore.Identity;

namespace DeviceStatusBeacon.Database;

public class User : IdentityUser<Guid> {
	/// <summary>
	/// 用户显示名称（可选）
	/// </summary>
	public string? DisplayName { get; set; }

	/// <summary>
	/// 该用户关联的主体角色列表，受唯一索引约束，每个用户最多只能关联一个角色，以匹配当前的四层权限模型
	/// </summary>
	/// <remarks>此集合由 EF Core 管理</remarks>
	public ICollection<UserRole> UserRoles { get; } = [];

	/// <summary>
	/// 该用户关联的 API 凭据列表
	/// </summary>
	/// <remarks>此集合由 EF Core 管理</remarks>
	public ICollection<ApiCredential> ApiCredentials { get; } = [];

	/// <summary>
	/// 该用户有权限查询的设备列表，仅适用于具有 <see cref="PrincipalRole.LimitedQuery"/> 角色的用户
	/// </summary>
	/// <remarks>此集合由 EF Core 管理</remarks>
	public ICollection<Device> AuthorizedDevices { get; } = [];

	/// <summary>
	/// 该用户有权限查询的设备授权关系列表。
	/// </summary>
	/// <remarks>此集合由 EF Core 管理</remarks>
	public ICollection<DeviceUser> AuthorizedDeviceLinks { get; } = [];

	/// <summary>
	/// 该用户代设备提交的日志列表
	/// </summary>
	/// <remarks>此集合由 EF Core 管理</remarks>
	public ICollection<OnlineLog> SubmittedOnlineLogs { get; } = [];
}


public class UserRole : IdentityUserRole<Guid> {
	/// <summary>
	/// 该用户角色条目关联的角色实体
	/// </summary>
	/// <remarks>此实体由 EF Core 管理</remarks>
	public IdentityRole<Guid> Role { get; set; } = null!;

	/// <summary>
	/// 该用户角色条目关联的用户实体
	/// </summary>
	/// <remarks>此实体由 EF Core 管理</remarks>
	public User User { get; set; } = null!;
}


public class DeviceUser {
	/// <summary>
	/// 授权设备 ID。
	/// </summary>
	public Guid AuthorizedDevicesDeviceId { get; set; }

	/// <summary>
	/// 授权设备实体。
	/// </summary>
	/// <remarks>此实体由 EF Core 管理</remarks>
	public Device AuthorizedDevice { get; set; } = null!;

	/// <summary>
	/// 被授权用户 ID。
	/// </summary>
	public Guid AuthorizedUsersId { get; set; }

	/// <summary>
	/// 被授权用户实体。
	/// </summary>
	/// <remarks>此实体由 EF Core 管理</remarks>
	public User AuthorizedUser { get; set; } = null!;
}