namespace DeviceStatusBeacon.Services;

public sealed partial class AccessAdministrationService {
	/// <inheritdoc/>
	public async Task<CreateUserCommandResult> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		EnsureValidUserName(command.UserName, "用户名不符合身份标识格式");
		EnsureDefinedRole(command.Role, "无效的用户角色");

		var newUser = new User {
			UserName = command.UserName,
			DisplayName = command.DisplayName
		};

		// 用户、角色和授权设备范围必须一起落库，否则会产生无角色用户或半写入的访问控制配置
		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

		EnsureIdentitySucceeded(await userManager.CreateAsync(newUser, command.Password));
		EnsureIdentitySucceeded(await userManager.AddToRoleAsync(newUser, command.Role.ToString()));
		await UpdateAuthorizedDeviceLinksAsync(newUser.Id, command.AuthorizedDeviceIds, true, cancellationToken);
		await dbContext.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return new(
			newUser.Id,
			newUser.UserName,
			newUser.DisplayName,
			command.Role);
	}

	/// <inheritdoc/>
	public async Task RenameUserAsync(Guid userId, RenameUserCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		EnsureValidUserName(command.NewUserName, "新用户名不符合身份标识格式");

		var user = await FindUserByIdAsync(userId);
		EnsureIdentitySucceeded(await userManager.SetUserNameAsync(user, command.NewUserName));
	}

	/// <inheritdoc/>
	public async Task RenameUserAsync(string userName, RenameUserCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		EnsureValidUserName(command.NewUserName, "新用户名不符合身份标识格式");

		var user = await FindUserByNameAsync(userName);
		EnsureIdentitySucceeded(await userManager.SetUserNameAsync(user, command.NewUserName));
	}

	/// <inheritdoc/>
	public async Task SetUserDisplayNameAsync(Guid userId, SetUserDisplayNameCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		await SetUserDisplayNameAsync(
			dbContext.Users.WhereUserId(userId),
			command.DisplayName,
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task SetUserDisplayNameAsync(string userName, SetUserDisplayNameCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		var userNameLookup = CreateUserNameLookup(userName);
		await SetUserDisplayNameAsync(
			dbContext.Users.WhereUserName(userNameLookup),
			command.DisplayName,
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task ResetUserPasswordAsync(Guid userId, ResetUserPasswordCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		var user = await FindUserByIdAsync(userId);

		// 管理员重置密码不要求知道旧密码，通过 Identity 的重置令牌走同一套密码校验规则
		var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
		EnsureIdentitySucceeded(await userManager.ResetPasswordAsync(user, resetToken, command.NewPassword));
	}

	/// <inheritdoc/>
	public async Task ResetUserPasswordAsync(string userName, ResetUserPasswordCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		var user = await FindUserByNameAsync(userName);

		// 管理员重置密码不要求知道旧密码，通过 Identity 的重置令牌走同一套密码校验规则
		var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
		EnsureIdentitySucceeded(await userManager.ResetPasswordAsync(user, resetToken, command.NewPassword));
	}

	/// <inheritdoc/>
	public async Task SetUserRoleAsync(Guid userId, SetUserRoleCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		var user = await FindUserByIdAsync(userId);
		await SetUserRoleAsync(user, command, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task SetUserRoleAsync(string userName, SetUserRoleCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		var user = await FindUserByNameAsync(userName);
		await SetUserRoleAsync(user, command, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task SetUserAuthorizedDevicesAsync(Guid userId, SetUserAuthorizedDevicesCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		await SetUserAuthorizedDevicesAsync(
			dbContext.Users.WhereUserId(userId),
			command,
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task SetUserAuthorizedDevicesAsync(string userName, SetUserAuthorizedDevicesCommand command, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(command);
		CommandValidation.EnsureValid(
			command,
			message => new AccessAdministrationException(StatusCodes.Status422UnprocessableEntity, message));

		var userNameLookup = CreateUserNameLookup(userName);
		await SetUserAuthorizedDevicesAsync(
			dbContext.Users.WhereUserName(userNameLookup),
			command,
			cancellationToken);
	}

	/// <inheritdoc/>
	public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default) {
		var user = await FindUserByIdAsync(userId);
		EnsureIdentitySucceeded(await userManager.DeleteAsync(user));
	}

	/// <inheritdoc/>
	public async Task DeleteUserAsync(string userName, CancellationToken cancellationToken = default) {
		var user = await FindUserByNameAsync(userName);
		EnsureIdentitySucceeded(await userManager.DeleteAsync(user));
	}

	/// <summary>
	/// 更新指定用户的显示名称。
	/// </summary>
	/// <param name="users">已经限定目标用户范围的用户查询</param>
	/// <param name="displayName">新的显示名称</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	private static async Task SetUserDisplayNameAsync(IQueryable<User> users, string? displayName, CancellationToken cancellationToken) {
		// 显示名称更新不需要加载完整用户实体，直接批量更新可以避免污染当前 DbContext 跟踪状态
		var updatedCount = await users.ExecuteUpdateAsync(
			user => user.SetProperty(entity => entity.DisplayName, displayName),
			cancellationToken);

		EnsureEntityFound(updatedCount, "未找到指定的用户");
	}

	/// <summary>
	/// 更新指定用户的角色，并在需要时收窄其 API 凭据权限范围。
	/// </summary>
	/// <param name="user">用户实体</param>
	/// <param name="command">用户角色更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	private async Task SetUserRoleAsync(User user, SetUserRoleCommand command, CancellationToken cancellationToken) {
		EnsureDefinedRole(command.Role, "无效的用户角色");

		// 获取当前的用户角色
		var currentRoles = await userManager.GetRolesAsync(user);

		// 如果当前角色与目标角色相同，则无需进行任何操作
		var targetRoleName = command.Role.ToString();
		if (currentRoles.Count == 1 && currentRoles[0] == targetRoleName) {
			return;
		}

		// 角色变更和 API 凭据收缩必须处在同一事务中，避免短暂出现凭据权限高于所属用户的状态
		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

		if (currentRoles.Count > 0) {
			// 移除当前角色
			EnsureIdentitySucceeded(await userManager.RemoveFromRolesAsync(user, currentRoles));
		}

		// 将用户添加到目标角色
		EnsureIdentitySucceeded(await userManager.AddToRoleAsync(user, targetRoleName));

		// 收窄相应的全部 API 凭据的权限范围（如果有的话），以匹配用户的新角色
		// 原先不存在角色、原先的角色有误、角色降级这三种情况下需要进行权限范围的收窄
		if (currentRoles.Count == 0 || !PrincipalRole.TryParse(currentRoles[0], out var currentRole) || command.Role < currentRole) {
			await ShrinkApiCredentialScopesAsync(user.Id, command.Role, cancellationToken);
		}

		await transaction.CommitAsync(cancellationToken);
	}

	/// <summary>
	/// 更新指定用户的授权设备范围，并在需要时收窄其 API 凭据权限范围。
	/// </summary>
	/// <param name="users">已经限定目标用户范围的用户查询</param>
	/// <param name="command">用户授权设备更新请求</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	private async Task SetUserAuthorizedDevicesAsync(IQueryable<User> users, SetUserAuthorizedDevicesCommand command, CancellationToken cancellationToken) {
		// 授权设备与派生的 API 凭据范围必须原子更新，避免 LimitedQuery 用户短暂看到超出范围的设备
		await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

		var user = await users
			.Select(entity => new {
				entity.Id,
				RoleName = entity.UserRoles.Select(userRole => userRole.Role.Name).SingleOrDefault()
			})
			.SingleOrDefaultAsync(cancellationToken)
			?? throw new AccessAdministrationException(StatusCodes.Status404NotFound, "未找到指定的用户");

		var userRole = PrincipalRole.TryParse(user.RoleName, out var parsedRole)
			? parsedRole
			: throw new AccessAdministrationException(StatusCodes.Status409Conflict, "目标用户未正确设置角色");

		var authorizedDeviceIds = await UpdateAuthorizedDeviceLinksAsync(user.Id, command.AuthorizedDeviceIds, false, cancellationToken);

		await (userRole switch {
			// 只有 LimitedQuery 用户的 API 凭据授权设备范围需要受用户设备列表约束
			PrincipalRole.LimitedQuery => ShrinkApiCredentialScopesAsync(user.Id, userRole, authorizedDeviceIds, cancellationToken),

			// 其他角色的用户无需裁剪 API 凭据授权设备范围
			_ => dbContext.SaveChangesAsync(cancellationToken)
		});

		await transaction.CommitAsync(cancellationToken);
	}
}