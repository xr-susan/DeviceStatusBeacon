using Microsoft.AspNetCore.Identity;

namespace DeviceStatusBeacon.Services;

/// <summary>
/// 访问管理服务。
/// </summary>
public sealed partial class AccessAdministrationService(
	DeviceStatusBeaconContext dbContext,
	UserManager<User> userManager,
	ILookupNormalizer lookupNormalizer,
	IDataProtectorV1 dataProtector) : IAccessAdministrationService {
	/// <summary>
	/// API 凭据所属用户摘要。
	/// </summary>
	/// <param name="UserId">用户 ID</param>
	/// <param name="Role">用户角色</param>
	private sealed record ApiCredentialOwner(
		Guid UserId,
		PrincipalRole Role
	);

	/// <summary>
	/// API 凭据管理目标摘要。
	/// </summary>
	/// <param name="ApiCredentialId">API 凭据 ID</param>
	/// <param name="Owner">API 凭据所属用户摘要</param>
	/// <param name="Role">API 凭据角色</param>
	private sealed record ApiCredentialTarget(
		Guid ApiCredentialId,
		ApiCredentialOwner Owner,
		PrincipalRole Role
	);

	/// <summary>
	/// 创建用户名查找条件。
	/// </summary>
	/// <param name="value">原始用户名</param>
	/// <returns>用户名查找条件</returns>
	private IdentityNameLookup CreateUserNameLookup(string value) =>
		IdentityNameLookup.TryCreate(value, lookupNormalizer)
			?? throw new AccessAdministrationException(StatusCodes.Status404NotFound, "未找到指定的用户");

	/// <summary>
	/// 创建设备名称查找条件。
	/// </summary>
	/// <param name="value">原始设备名称</param>
	/// <returns>设备名称查找条件</returns>
	private IdentityNameLookup CreateDeviceNameLookup(string value) =>
		IdentityNameLookup.TryCreate(value, lookupNormalizer)
			?? throw new AccessAdministrationException(StatusCodes.Status404NotFound, "未找到指定的设备");

	/// <summary>
	/// 确保用户名符合身份标识格式。
	/// </summary>
	/// <param name="userName">用户名</param>
	/// <param name="message">格式错误时使用的错误消息</param>
	private static void EnsureValidUserName(string userName, string message) {
		if (!IdentityNameRules.IsValid(userName)) {
			throw new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message);
		}
	}

	/// <summary>
	/// 确保写入语句命中了目标用户或 API 凭据。
	/// </summary>
	/// <param name="affectedCount">写入影响的行数</param>
	/// <param name="message">未命中目标时使用的错误消息</param>
	private static void EnsureEntityFound(int affectedCount, string message) {
		if (affectedCount == 0) {
			throw new AccessAdministrationException(StatusCodes.Status404NotFound, message);
		}
	}

	/// <summary>
	/// 将 Identity 操作失败转换为统一的服务层业务异常。
	/// </summary>
	/// <param name="result">Identity 操作结果</param>
	private static void EnsureIdentitySucceeded(IdentityResult result) {
		if (result.Succeeded) {
			return;
		}

		// Identity 的错误码比异常类型更稳定，这里只把唯一性冲突提升为 409，其余校验失败统一视作 422
		var statusCode = result.Errors.Any(error =>
			string.Equals(error.Code, nameof(IdentityErrorDescriber.DuplicateUserName), StringComparison.Ordinal)
			|| string.Equals(error.Code, nameof(IdentityErrorDescriber.DuplicateEmail), StringComparison.Ordinal))
			? StatusCodes.Status409Conflict
			: StatusCodes.Status422UnprocessableEntity;

		throw new AccessAdministrationException(statusCode, string.Join(Environment.NewLine, result.Errors.Select(error => error.Description)));
	}

	/// <summary>
	/// 根据用户 ID 查询用户。
	/// </summary>
	/// <param name="userId">用户 ID</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>用户实体</returns>
	private async Task<User> FindUserByIdAsync(Guid userId) =>
		// UserManager 负责 Identity 用户读取；此处保留统一的服务层 404 语义
		await userManager.FindByIdAsync(userId.ToString())
			?? throw new AccessAdministrationException(StatusCodes.Status404NotFound, "未找到指定的用户");

	/// <summary>
	/// 根据用户名查询用户。
	/// </summary>
	/// <param name="userName">用户名</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>用户实体</returns>
	private async Task<User> FindUserByNameAsync(string userName) =>
		// UserManager 会按 Identity 的用户名归一化规则查找，避免服务层重复实现用户名匹配规则
		await userManager.FindByNameAsync(userName)
			?? throw new AccessAdministrationException(StatusCodes.Status404NotFound, "未找到指定的用户");

	/// <summary>
	/// 查询 API 凭据所属用户与角色。
	/// </summary>
	/// <remarks>
	/// 修改 API 凭据前必须读取凭据角色与所属用户角色，因为凭据角色不能高于所属用户角色。
	/// </remarks>
	/// <param name="apiCredentialId">API 凭据 ID</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>API 凭据管理目标摘要</returns>
	private async Task<ApiCredentialTarget> GetApiCredentialTargetAsync(Guid apiCredentialId, CancellationToken cancellationToken) {
		var target = await dbContext.ApiCredentials
			.AsNoTracking()
			.WhereApiCredentialId(apiCredentialId)
			.Select(credential => new {
				credential.ApiCredentialId,
				credential.UserId,
				credential.Role,
				RoleName = credential.User.UserRoles.Select(userRole => userRole.Role.Name).SingleOrDefault()
			})
			.SingleOrDefaultAsync(cancellationToken)
			?? throw new AccessAdministrationException(StatusCodes.Status404NotFound, "未找到指定的 API 凭据");

		return PrincipalRole.TryParse(target.RoleName, out var ownerRole)
			? new(target.ApiCredentialId, new(target.UserId, ownerRole), target.Role)
			: throw new AccessAdministrationException(StatusCodes.Status409Conflict, "API 凭据所属用户未正确设置角色");
	}

	/// <summary>
	/// 查询 API 凭据所属用户与角色。
	/// </summary>
	/// <param name="users">已经限定所属用户范围的用户查询</param>
	/// <param name="notFoundMessage">未找到所属用户时使用的错误消息</param>
	/// <param name="invalidRoleMessage">所属用户角色无效时使用的错误消息</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>API 凭据所属用户摘要</returns>
	private static async Task<ApiCredentialOwner> GetApiCredentialOwnerAsync(
		IQueryable<User> users,
		string notFoundMessage,
		string invalidRoleMessage,
		CancellationToken cancellationToken) {
		var owner = await users
			.AsNoTracking()
			.Select(user => new {
				user.Id,
				RoleName = user.UserRoles.Select(userRole => userRole.Role.Name).SingleOrDefault()
			})
			.SingleOrDefaultAsync(cancellationToken)
			?? throw new AccessAdministrationException(StatusCodes.Status404NotFound, notFoundMessage);

		return PrincipalRole.TryParse(owner.RoleName, out var ownerRole)
			? new(owner.Id, ownerRole)
			: throw new AccessAdministrationException(StatusCodes.Status409Conflict, invalidRoleMessage);
	}

	/// <summary>
	/// 确保角色值为已定义的主体角色。
	/// </summary>
	/// <param name="role">待校验的角色值</param>
	/// <param name="message">角色无效时使用的错误消息</param>
	private static void EnsureDefinedRole(PrincipalRole role, string message) {
		if (!role.IsDefined()) {
			throw new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message);
		}
	}

	/// <summary>
	/// 确保 API 凭据角色不高于所属用户角色。
	/// </summary>
	/// <param name="role">API 凭据角色</param>
	/// <param name="ownerRole">所属用户角色</param>
	private static void EnsureApiCredentialRoleWithinOwnerRole(PrincipalRole role, PrincipalRole ownerRole) {
		EnsureDefinedRole(role, "无效的 API 凭据角色");

		if (role > ownerRole) {
			throw new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, "API 凭据角色不得高于所属用户角色");
		}
	}
}