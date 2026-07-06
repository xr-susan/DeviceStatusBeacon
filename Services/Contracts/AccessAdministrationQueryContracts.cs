namespace DeviceStatusBeacon.Services.Contracts;

/// <summary>
/// 访问管理用户列表页数据。
/// </summary>
/// <param name="Pagination">分页数据</param>
/// <param name="Users">用户列表</param>
public sealed record UserListData(
	PaginationData Pagination,
	IReadOnlyCollection<AccessUserSummary> Users
);

/// <summary>
/// 访问管理 API 凭据列表页数据。
/// </summary>
/// <param name="Pagination">分页数据</param>
/// <param name="ApiCredentials">API 凭据列表</param>
public sealed record ApiCredentialListData(
	PaginationData Pagination,
	IReadOnlyCollection<AccessApiCredentialSummary> ApiCredentials
);

/// <summary>
/// 访问管理设备授权用户数据。
/// </summary>
/// <param name="DeviceId">设备 ID</param>
/// <param name="DeviceName">设备名称</param>
/// <param name="DisplayName">设备显示名称</param>
/// <param name="Pagination">分页数据</param>
/// <param name="AuthorizedUsers">授权用户列表</param>
public sealed record DeviceAuthorizedUsersData(
	Guid DeviceId,
	string DeviceName,
	string? DisplayName,
	PaginationData Pagination,
	IReadOnlyCollection<AccessUserSummary> AuthorizedUsers
);

/// <summary>
/// 访问管理用户摘要。
/// </summary>
/// <param name="UserId">用户 ID</param>
/// <param name="UserName">用户名</param>
/// <param name="DisplayName">显示名称</param>
/// <param name="RoleName">角色名称</param>
/// <param name="AuthorizedDeviceCount">授权设备数量</param>
/// <param name="ApiCredentialCount">API 凭据数量</param>
public sealed record AccessUserSummary(
	Guid UserId,
	string UserName,
	string? DisplayName,
	string? RoleName,
	int AuthorizedDeviceCount,
	int ApiCredentialCount
) {
	/// <summary>
	/// 用户角色。
	/// </summary>
	public PrincipalRole? Role => field ?? (PrincipalRole.TryParseOrNull(RoleName, out field) ? field : null);
}

/// <summary>
/// 访问管理 API 凭据摘要。
/// </summary>
/// <param name="ApiCredentialId">API 凭据 ID</param>
/// <param name="DisplayName">显示名称</param>
/// <param name="Enabled">是否启用</param>
/// <param name="Role">API 凭据角色</param>
/// <param name="OwnerUserId">所属用户 ID</param>
/// <param name="OwnerUserName">所属用户名</param>
/// <param name="OwnerDisplayName">所属用户显示名称</param>
/// <param name="OwnerRoleName">所属用户角色名称</param>
/// <param name="AuthorizedDeviceCount">授权设备数量</param>
public sealed record AccessApiCredentialSummary(
	Guid ApiCredentialId,
	string DisplayName,
	bool Enabled,
	PrincipalRole Role,
	Guid OwnerUserId,
	string OwnerUserName,
	string? OwnerDisplayName,
	string? OwnerRoleName,
	int AuthorizedDeviceCount
) {
	/// <summary>
	/// 所属用户角色。
	/// </summary>
	public PrincipalRole? OwnerRole => field ?? (PrincipalRole.TryParseOrNull(OwnerRoleName, out field) ? field : null);
}