using System.ComponentModel.DataAnnotations;

namespace DeviceStatusBeacon.Services.Contracts;

/// <summary>
/// 用户创建请求。
/// </summary>
public sealed class CreateUserCommand {
	/// <summary>
	/// 用户名。
	/// </summary>
	[Required]
	public required string UserName { get; init; }

	/// <summary>
	/// 用户初始密码。
	/// </summary>
	[Required]
	public required string Password { get; init; }

	/// <summary>
	/// 用户角色。
	/// </summary>
	/// <remarks>调用方应只传入 <see cref="PrincipalRole"/> 中已定义的角色值。</remarks>
	[Required]
	public required PrincipalRole Role { get; init; }

	/// <summary>
	/// 用户显示名称。
	/// </summary>
	public string? DisplayName { get; init; }

	/// <summary>
	/// 用户有权限查询的设备 ID 列表，仅在 <see cref="Role"/> 为 <see cref="PrincipalRole.LimitedQuery"/> 时生效。
	/// </summary>
	/// <remarks>用户角色高于 <see cref="PrincipalRole.LimitedQuery"/> 时，此列表将如实落库，但不会影响用户的查询权限。</remarks>
	public IReadOnlyCollection<Guid>? AuthorizedDeviceIds { get; init; }
}

/// <summary>
/// 用户重命名请求。
/// </summary>
public sealed class RenameUserCommand {
	/// <summary>
	/// 新用户名。
	/// </summary>
	[Required]
	public required string NewUserName { get; init; }
}

/// <summary>
/// 用户显示名称更新请求。
/// </summary>
public sealed class SetUserDisplayNameCommand {
	/// <summary>
	/// 新显示名称。
	/// </summary>
	/// <remarks>设为 null 表示清空显示名称。</remarks>
	public required string? DisplayName { get; init; }
}

/// <summary>
/// 用户密码重置请求。
/// </summary>
public sealed class ResetUserPasswordCommand {
	/// <summary>
	/// 新密码。
	/// </summary>
	[Required]
	public required string NewPassword { get; init; }
}

/// <summary>
/// 用户角色更新请求。
/// </summary>
public sealed class SetUserRoleCommand {
	/// <summary>
	/// 新角色。
	/// </summary>
	/// <remarks>调用方应只传入 <see cref="PrincipalRole"/> 中已定义的角色值。</remarks>
	[Required]
	public required PrincipalRole Role { get; init; }
}

/// <summary>
/// 用户授权设备更新请求。
/// </summary>
public sealed class SetUserAuthorizedDevicesCommand {
	/// <summary>
	/// 用户有权限查询的设备 ID 列表，仅在用户角色为 <see cref="PrincipalRole.LimitedQuery"/> 时生效。
	/// </summary>
	/// <remarks>用户角色高于 <see cref="PrincipalRole.LimitedQuery"/> 时，此列表将如实落库，但不会影响用户的查询权限。</remarks>
	[Required]
	public required IReadOnlyCollection<Guid> AuthorizedDeviceIds { get; init; }
}

/// <summary>
/// API 凭据创建请求。
/// </summary>
public sealed class CreateApiCredentialCommand {
	/// <summary>
	/// API 凭据角色。
	/// </summary>
	/// <remarks>调用方应只传入 <see cref="PrincipalRole"/> 中已定义的角色值。</remarks>
	[Required]
	public required PrincipalRole Role { get; init; }

	/// <summary>
	/// API 凭据显示名称。
	/// </summary>
	public string? DisplayName { get; init; }

	/// <summary>
	/// API 凭据有权限查询的设备 ID 列表，仅在 <see cref="Role"/> 为 <see cref="PrincipalRole.LimitedQuery"/> 时生效。
	/// </summary>
	/// <remarks>凭据角色高于 <see cref="PrincipalRole.LimitedQuery"/> 时，此列表将如实落库，但不会影响凭据的查询权限。</remarks>
	public IReadOnlyCollection<Guid>? AuthorizedDeviceIds { get; init; }
}

/// <summary>
/// API 凭据显示名称更新请求。
/// </summary>
public sealed class SetApiCredentialDisplayNameCommand {
	/// <summary>
	/// 新显示名称。
	/// </summary>
	/// <remarks>设为 null 表示清空显示名称。</remarks>
	public required string? DisplayName { get; init; }
}

/// <summary>
/// API 凭据启用状态更新请求。
/// </summary>
public sealed class SetApiCredentialEnabledCommand {
	/// <summary>
	/// 是否启用 API 凭据。
	/// </summary>
	[Required]
	public required bool Enabled { get; init; }
}

/// <summary>
/// API 凭据角色更新请求。
/// </summary>
public sealed class SetApiCredentialRoleCommand {
	/// <summary>
	/// 新角色。
	/// </summary>
	/// <remarks>调用方应只传入 <see cref="PrincipalRole"/> 中已定义的角色值。</remarks>
	[Required]
	public required PrincipalRole Role { get; init; }
}

/// <summary>
/// API 凭据授权设备更新请求。
/// </summary>
public sealed class SetApiCredentialAuthorizedDevicesCommand {
	/// <summary>
	/// API 凭据有权限查询的设备 ID 列表，仅在 API 凭据角色为 <see cref="PrincipalRole.LimitedQuery"/> 时生效。
	/// </summary>
	/// <remarks>凭据角色高于 <see cref="PrincipalRole.LimitedQuery"/> 时，此列表将如实落库，但不会影响凭据的查询权限。</remarks>
	[Required]
	public required IReadOnlyCollection<Guid> AuthorizedDeviceIds { get; init; }
}

/// <summary>
/// 设备授权用户更新请求。
/// </summary>
public sealed class SetDeviceAuthorizedUsersCommand {
	/// <summary>
	/// 有权限查询该设备数据的用户 ID 列表，仅在用户角色为 <see cref="PrincipalRole.LimitedQuery"/> 时生效。
	/// </summary>
	/// <remarks>列表中存在用户角色高于 <see cref="PrincipalRole.LimitedQuery"/> 时，此列表将如实落库，但不会影响该用户的查询权限。</remarks>
	[Required]
	public required IReadOnlyCollection<Guid> AuthorizedUserIds { get; init; }
}

/// <summary>
/// 用户创建结果。
/// </summary>
/// <param name="UserId">新用户 ID</param>
/// <param name="UserName">用户名</param>
/// <param name="DisplayName">用户显示名称</param>
/// <param name="Role">用户角色</param>
public sealed record CreateUserCommandResult(
	Guid UserId,
	string UserName,
	string? DisplayName,
	PrincipalRole Role
);

/// <summary>
/// API 凭据创建结果。
/// </summary>
/// <param name="ApiCredentialId">新 API 凭据 ID</param>
/// <param name="SecretKey">API 凭据的操作密钥，只在创建结果中返回一次</param>
public sealed record CreateApiCredentialCommandResult(
	Guid ApiCredentialId,
	string SecretKey
);

/// <summary>
/// API 凭据密钥重置结果。
/// </summary>
/// <param name="SecretKey">API 凭据的新操作密钥，只在重置结果中返回一次</param>
public sealed record ResetApiCredentialSecretKeyCommandResult(
	string SecretKey
);