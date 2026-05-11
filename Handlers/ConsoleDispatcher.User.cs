using Microsoft.AspNetCore.Identity;

namespace DeviceStatusBeacon.Handlers;

/// <inheritdoc/>
public static partial class ConsoleDispatcher {
	/// <summary>
	/// 处理 user 命令
	/// </summary>
	/// <param name="argsAfterVerb">动词之后的参数</param>
	/// <param name="sp">负责依赖注入的服务提供者</param>
	/// <returns>一个表示异步操作的任务，任务结果指示应用程序的退出代码</returns>
	private static async Task<int> HandleUserCommandAsync(string[] argsAfterVerb, IServiceProvider sp) {
		await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();
		using var userManager = sp.GetRequiredService<UserManager<User>>();
		var normalizer = sp.GetRequiredService<ILookupNormalizer>();

		// user list
		if (argsAfterVerb is ["list"]) {
			return await HandleUserListAsync(db);
		}

		// user query <part-of-name>
		if (argsAfterVerb is ["query", var partOfName]) {
			return await HandleUserQueryAsync(db, normalizer, partOfName);
		}

		// user add <name> <role> <password>
		if (argsAfterVerb is ["add", var userNameToAdd, var roleString, var password]) {
			return await HandleUserAddAsync(db, userManager, userNameToAdd, roleString, password);
		}

		// user reset-password <name> <new-password>
		if (argsAfterVerb is ["reset-password", var userNameToReset, var newPassword]) {
			return await HandleUserResetPasswordAsync(db, userManager, userNameToReset, newPassword);
		}

		// user rename <old-name> <new-name>
		if (argsAfterVerb is ["rename", var oldUserName, var newUserName]) {
			return await HandleUserRenameAsync(db, userManager, oldUserName, newUserName);
		}

		// user set-role <name> <role>
		if (argsAfterVerb is ["set-role", var userNameToSetRole, var roleStringToSet]) {
			return await HandleUserSetRoleAsync(db, userManager, userNameToSetRole, roleStringToSet);
		}

		// user delete <name>
		if (argsAfterVerb is ["delete", var userNameToDelete]) {
			return await HandleUserDeleteAsync(db, userManager, userNameToDelete);
		}

		Console.WriteLine("无效的 user 命令参数。");
		return 3;
	}

	/// <summary>
	/// 处理 user list 命令，列出所有用户的 ID、用户名、显示名称和角色
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserListAsync(DeviceStatusBeaconContext db) {
		var users = await db.Users
			.AsNoTracking()
			.OrderBy(u => u.NormalizedUserName)
			.Select(u => new {
				u.Id,
				u.UserName,
				u.DisplayName,
				Role = u.UserRoles.Select(ur => ur.Role.Name).SingleOrDefault() ?? "<NoRole>"
			})
			.Take(MaxDisplayCount + 1)
			.ToListAsync();

		return PrintListWithSummary(
			users,
			"没有找到任何用户",
			$"用户数量过多（超过 {MaxDisplayCount} 个），请使用 query 命令或数据库管理工具查询",
			"用户",
			user => Console.WriteLine($"  [{user.Id}] {user.UserName} ({user.DisplayName ?? "<NoDisplayName>"}, {user.Role})")
		);
	}

	/// <summary>
	/// 处理 user query <part-of-name> 命令，列出所有用户名包含指定字符串的用户的 ID、用户名、显示名称和角色
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="normalizer">用于规范化用户名的工具</param>
	/// <param name="partOfName">要查询的用户名部分</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserQueryAsync(DeviceStatusBeaconContext db, ILookupNormalizer normalizer, string partOfName) {
		var normalizedPartOfName = normalizer.NormalizeName(partOfName);

		var users = await db.Users
			.AsNoTracking()
			.Where(u => u.NormalizedUserName != null // skipcq: CS-R1136 表达式树不支持 is 模式匹配
				&& u.NormalizedUserName.Contains(normalizedPartOfName))
			.OrderBy(u => u.NormalizedUserName)
			.Select(u => new {
				u.Id,
				u.UserName,
				u.DisplayName,
				Role = u.UserRoles.Select(ur => ur.Role.Name).SingleOrDefault() ?? "<NoRole>"
			})
			.Take(MaxDisplayCount + 1)
			.ToListAsync();

		return PrintListWithSummary(
			users,
			"没有找到匹配的用户",
			$"匹配的用户数量过多（超过 {MaxDisplayCount} 个），请使用更精确的查询条件",
			"用户",
			user => Console.WriteLine($"  [{user.Id}] {user.UserName} ({user.DisplayName ?? "<NoDisplayName>"}, {user.Role})")
		);
	}

	/// <summary>
	/// 处理 user add <name> <role> <password> 命令，添加一个新用户并为其分配指定的角色和密码
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="userManager">用户管理器</param>
	/// <param name="userNameToAdd">要添加的用户名</param>
	/// <param name="roleString">要分配的角色字符串</param>
	/// <param name="password">用户的密码</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserAddAsync(DeviceStatusBeaconContext db, UserManager<User> userManager, string userNameToAdd, string roleString, string password) {
		if (!GeneratedRegex.IdentityRegex().IsMatch(userNameToAdd)) {
			Console.WriteLine("用户名不符合身份标识格式");
			return 3;
		}

		if (!Enum.TryParse(roleString, true, out PrincipalRole role)) {
			Console.WriteLine("无效的用户角色");
			return 3;
		}

		var newUser = new User {
			UserName = userNameToAdd
		};

		await using var transaction = await db.Database.BeginTransactionAsync();

		var createResult = await userManager.CreateAsync(newUser, password);
		if (!createResult.Succeeded) {
			return PrintIdentityErrors(createResult);
		}

		var addRoleResult = await userManager.AddToRoleAsync(newUser, role.ToString());
		if (!addRoleResult.Succeeded) {
			return PrintIdentityErrors(addRoleResult);
		}

		// 提前提交事务，不理会后续 UpdateEntityAuthInfoVersionInternalAsync 的结果
		await transaction.CommitAsync();

		Console.WriteLine($"""
			用户添加成功：
			  用户 ID：{newUser.Id}
			  用户名：{newUser.UserName}
			  角色：{role}
			""");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}

	/// <summary>
	/// 处理 user reset-password <name> <new-password> 命令，重置指定用户的密码
	/// </summary>
	/// <param name="userManager">用户管理器</param>
	/// <param name="userNameToReset">要重置密码的用户名</param>
	/// <param name="newPassword">新的密码</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserResetPasswordAsync(DeviceStatusBeaconContext db, UserManager<User> userManager, string userNameToReset, string newPassword) {
		var user = await userManager.FindByNameAsync(userNameToReset);
		if (user is null) {
			Console.WriteLine("未找到指定的用户");
			return 2;
		}

		var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
		var resetResult = await userManager.ResetPasswordAsync(user, resetToken, newPassword);
		if (!resetResult.Succeeded) {
			return PrintIdentityErrors(resetResult);
		}

		Console.WriteLine($"用户 {userNameToReset} 的密码已重置");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}

	/// <summary>
	/// 处理 user rename <old-name> <new-name> 命令，重命名指定的用户
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="userManager">用户管理器</param>
	/// <param name="oldUserName">要重命名的旧用户名</param>
	/// <param name="newUserName">新的用户名</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserRenameAsync(DeviceStatusBeaconContext db, UserManager<User> userManager, string oldUserName, string newUserName) {
		if (!GeneratedRegex.IdentityRegex().IsMatch(newUserName)) {
			Console.WriteLine("新用户名不符合身份标识格式");
			return 3;
		}

		var user = await userManager.FindByNameAsync(oldUserName);
		if (user is null) {
			Console.WriteLine("未找到指定的用户");
			return 2;
		}

		var renameResult = await userManager.SetUserNameAsync(user, newUserName);
		if (!renameResult.Succeeded) {
			return PrintIdentityErrors(renameResult);
		}

		Console.WriteLine($"用户重命名成功：{oldUserName} -> {newUserName}");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}

	/// <summary>
	/// 处理 user set-role <name> <role> 命令，为指定用户设置新角色，并根据新角色收窄相应的 API 凭据的权限范围
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="userManager">用户管理器</param>
	/// <param name="userNameToSetRole">要设置角色的用户名</param>
	/// <param name="roleStringToSet">要设置的角色字符串</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserSetRoleAsync(DeviceStatusBeaconContext db, UserManager<User> userManager, string userNameToSetRole, string roleStringToSet) {
		if (!Enum.TryParse(roleStringToSet, true, out PrincipalRole role)) {
			Console.WriteLine("无效的用户角色");
			return 3;
		}

		var user = await userManager.FindByNameAsync(userNameToSetRole);
		if (user is null) {
			Console.WriteLine("未找到指定的用户");
			return 2;
		}

		var targetRoleName = role.ToString();
		var currentRoles = await userManager.GetRolesAsync(user);
		if (currentRoles.Count == 1 && currentRoles[0] == targetRoleName) {
			Console.WriteLine($"用户 {userNameToSetRole} 的角色已经是：{role}");
			return 0;
		}

		await using var transaction = await db.Database.BeginTransactionAsync();

		// 移除用户当前的角色
		if (currentRoles.Count > 0) {
			var removeRoleResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
			if (!removeRoleResult.Succeeded) {
				return PrintIdentityErrors(removeRoleResult);
			}
		}

		// 添加用户的新角色
		var addRoleResult = await userManager.AddToRoleAsync(user, targetRoleName);
		if (!addRoleResult.Succeeded) {
			return PrintIdentityErrors(addRoleResult);
		}

		// 收窄相应的全部 API 凭据的权限范围（如果有的话），以匹配用户的新角色
		// 原先不存在角色、原先的角色有误、角色降级这三种情况下需要进行权限范围的收窄
		if (currentRoles.Count == 0 || !Enum.TryParse(currentRoles[0], true, out PrincipalRole currentRole) || role < currentRole) {
			await ShrinkApiCredentialScopesAsync(db, user.Id, role);
		}

		// 提前提交事务，不理会后续 UpdateEntityAuthInfoVersionInternalAsync 的结果
		await transaction.CommitAsync();

		Console.WriteLine($"用户 {userNameToSetRole} 的角色已更新为：{role}");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}

	/// <summary>
	/// 处理 user delete <name> 命令，删除指定的用户，并删除与该用户相关的全部 API 凭据
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="userManager">用户管理器</param>
	/// <param name="userNameToDelete">要删除的用户名</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserDeleteAsync(DeviceStatusBeaconContext db, UserManager<User> userManager, string userNameToDelete) {
		var user = await userManager.FindByNameAsync(userNameToDelete);
		if (user is null) {
			Console.WriteLine("未找到指定的用户");
			return 2;
		}

		var deleteResult = await userManager.DeleteAsync(user);
		if (!deleteResult.Succeeded) {
			return PrintIdentityErrors(deleteResult);
		}

		Console.WriteLine($"用户 {userNameToDelete} 已删除");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}

	private static int PrintIdentityErrors(IdentityResult result) {
		Console.WriteLine("操作 ASP.NET Core Identity 用户失败，错误信息：");
		Console.WriteLine(string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)));
		return -1;
	}

	/// <summary>
	/// 收窄关联指定用户的 API 凭据的权限范围以匹配用户的新角色
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="userId">用户 ID</param>
	/// <param name="newRole">用户的新角色</param>
	/// <returns>一个表示异步操作的任务</returns>
	private static async Task ShrinkApiCredentialScopesAsync(DeviceStatusBeaconContext db, Guid userId, PrincipalRole newRole) {
		List<ApiCredential>? apiCredentialsToUpdate = null;

		// 仅在用户被降级为 LimitedQuery 角色时才会被赋值
		HashSet<Guid>? userDeviceIds = null;

		if (newRole == PrincipalRole.LimitedQuery) {
			// 通过用户 ID 查询用户授权的设备 ID 列表，利用索引加速查询
			// 此查询结果一定不会被修改，使用 AsNoTracking 来提升性能
			userDeviceIds = await db.Devices
				.AsNoTracking()
				.Where(d => d.AuthorizedUsers.Any(u => u.Id == userId))
				.Select(d => d.DeviceId)
				.ToHashSetAsync();

			// 通过用户 ID 查询相关的 API 凭据，利用索引加速查询
			apiCredentialsToUpdate = await db.ApiCredentials
				.Where(c => c.UserId == userId)
				.Include(c => c.AuthorizedDevices)
				.ToListAsync();
		} else {
			// 通过用户 ID 查询相关的 API 凭据，利用索引加速查询
			apiCredentialsToUpdate = await db.ApiCredentials
				.Where(c => c.UserId == userId)
				.ToListAsync();
		}

		foreach (var credential in apiCredentialsToUpdate) {
			// 将 API 凭据的角色降级为用户的新角色，以匹配用户的新角色
			if (credential.Role > newRole) {
				credential.Role = newRole;
			}

			// 如果用户被降级为 LimitedQuery 角色
			if (newRole == PrincipalRole.LimitedQuery) {
				// 逐个检查 AuthorizedDevices 列表是否超过所属用户的 AuthorizedDevices，如果存在超过的情况，则移除这些设备的授权
				var devicesToRemove = credential.AuthorizedDevices
					.Where(ad => !userDeviceIds!.Contains(ad.DeviceId))
					.ToList();

				if (devicesToRemove.Count > 0) {
					foreach (var deviceToRemove in devicesToRemove) {
						credential.AuthorizedDevices.Remove(deviceToRemove);
					}
				}
			}
		}

		await db.SaveChangesAsync();
	}
}