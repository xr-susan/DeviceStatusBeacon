namespace DeviceStatusBeacon;

/// <inheritdoc/>
public static partial class ConsoleDispatcher {
	/// <summary>
	/// 处理 user 命令
	/// </summary>
	/// <param name="argsAfterVerb">动词之后的参数</param>
	/// <param name="sp">负责依赖注入的服务提供者</param>
	/// <returns>一个表示异步操作的任务，任务结果指示应用程序的退出代码</returns>
	private static async Task<int> HandleUserCommandAsync(string[] argsAfterVerb, IServiceProvider sp) {
		var accessAdministrationQueryService = sp.GetRequiredService<IAccessAdministrationQueryService>();
		var accessAdministrationService = sp.GetRequiredService<IAccessAdministrationService>();

		return argsAfterVerb switch {
			// user list
			["list"] => await HandleUserListAsync(accessAdministrationQueryService),

			// user query <part-of-name>
			["query", var partOfName] => await HandleUserQueryAsync(accessAdministrationQueryService, partOfName),

			// user add <name> <role> <password>
			["add", var userName, var roleString, var password] => await HandleUserAddAsync(accessAdministrationService, userName, roleString, password),

			// user reset-password <name> <new-password>
			["reset-password", var userName, var newPassword] => await HandleUserResetPasswordAsync(accessAdministrationService, userName, newPassword),

			// user rename <old-name> <new-name>
			["rename", var oldUserName, var newUserName] => await HandleUserRenameAsync(accessAdministrationService, oldUserName, newUserName),

			// user set-role <name> <role>
			["set-role", var userName, var roleString] => await HandleUserSetRoleAsync(accessAdministrationService, userName, roleString),

			// user delete <name>
			["delete", var userName] => await HandleUserDeleteAsync(accessAdministrationService, userName),

			// 无效的 user 命令参数
			_ => ExitWithInvalidCommandMessage("user")
		};
	}

	/// <summary>
	/// 处理 user list 命令，列出所有用户的 ID、用户名、显示名称和角色
	/// </summary>
	/// <param name="accessAdministrationQueryService">访问管理查询服务</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserListAsync(IAccessAdministrationQueryService accessAdministrationQueryService) {
		var users = await accessAdministrationQueryService.GetUsersForConsoleAsync();

		return PrintListWithSummary(
			users,
			"没有找到任何用户",
			$"用户数量过多（超过 {MaxDisplayCount} 个），请使用 query 命令或数据库管理工具查询",
			"用户",
			user => Console.WriteLine($"  [{user.UserId}] {user.UserName} ({user.DisplayName ?? "<NoDisplayName>"}, {user.RoleName ?? "<NoRole>"})")
		);
	}

	/// <summary>
	/// 处理 user query <part-of-name> 命令，列出所有用户名包含指定字符串的用户的 ID、用户名、显示名称和角色
	/// </summary>
	/// <param name="accessAdministrationQueryService">访问管理查询服务</param>
	/// <param name="partOfName">要查询的用户名部分</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserQueryAsync(IAccessAdministrationQueryService accessAdministrationQueryService, string partOfName) {
		var users = await accessAdministrationQueryService.GetUsersForConsoleAsync(partOfName);

		return PrintListWithSummary(
			users,
			"没有找到匹配的用户",
			$"匹配的用户数量过多（超过 {MaxDisplayCount} 个），请使用更精确的查询条件",
			"用户",
			user => Console.WriteLine($"  [{user.UserId}] {user.UserName} ({user.DisplayName ?? "<NoDisplayName>"}, {user.RoleName ?? "<NoRole>"})")
		);
	}

	/// <summary>
	/// 处理 user add <name> <role> <password> 命令，添加一个新用户并为其分配指定的角色和密码
	/// </summary>
	/// <param name="accessAdministrationService">访问管理服务</param>
	/// <param name="userName">要添加的用户名</param>
	/// <param name="roleString">要分配的角色字符串</param>
	/// <param name="password">用户的密码</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserAddAsync(IAccessAdministrationService accessAdministrationService, string userName, string roleString, string password) {
		if (!PrincipalRole.TryParse(roleString, out var role)) {
			Console.WriteLine("无效的用户角色");
			return 3;
		}

		try {
			var commandResult = await accessAdministrationService.CreateUserAsync(new() {
				UserName = userName,
				Password = password,
				Role = role
			});

			Console.WriteLine($"""
				用户添加成功：
				  用户 ID：{commandResult.UserId}
				  用户名：{commandResult.UserName}
				  角色：{commandResult.Role}
				""");

			return 0;
		} catch (AccessAdministrationException e) {
			return ExitWithAccessAdministrationError(e);
		}
	}

	/// <summary>
	/// 处理 user reset-password <name> <new-password> 命令，重置指定用户的密码
	/// </summary>
	/// <param name="accessAdministrationService">访问管理服务</param>
	/// <param name="userName">要重置密码的用户名</param>
	/// <param name="newPassword">新的密码</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserResetPasswordAsync(IAccessAdministrationService accessAdministrationService, string userName, string newPassword) {
		try {
			await accessAdministrationService.ResetUserPasswordAsync(userName, new() {
				NewPassword = newPassword
			});

			Console.WriteLine($"用户 {userName} 的密码已重置");

			return 0;
		} catch (AccessAdministrationException e) {
			return ExitWithAccessAdministrationError(e);
		}
	}

	/// <summary>
	/// 处理 user rename <old-name> <new-name> 命令，重命名指定的用户
	/// </summary>
	/// <param name="accessAdministrationService">访问管理服务</param>
	/// <param name="oldUserName">要重命名的旧用户名</param>
	/// <param name="newUserName">新的用户名</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserRenameAsync(IAccessAdministrationService accessAdministrationService, string oldUserName, string newUserName) {
		try {
			await accessAdministrationService.RenameUserAsync(oldUserName, new() {
				NewUserName = newUserName
			});

			Console.WriteLine($"用户重命名成功：{oldUserName} -> {newUserName}");

			return 0;
		} catch (AccessAdministrationException e) {
			return ExitWithAccessAdministrationError(e);
		}
	}

	/// <summary>
	/// 处理 user set-role <name> <role> 命令，为指定用户设置新角色，并在需要时收窄相应 API 凭据的权限范围
	/// </summary>
	/// <param name="accessAdministrationService">访问管理服务</param>
	/// <param name="userName">要设置角色的用户名</param>
	/// <param name="roleString">要设置的角色字符串</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserSetRoleAsync(IAccessAdministrationService accessAdministrationService, string userName, string roleString) {
		if (!PrincipalRole.TryParse(roleString, out var role)) {
			Console.WriteLine("无效的用户角色");
			return 3;
		}

		try {
			await accessAdministrationService.SetUserRoleAsync(userName, new() {
				Role = role
			});

			Console.WriteLine($"用户 {userName} 的角色已更新为：{role}");

			return 0;
		} catch (AccessAdministrationException e) {
			return ExitWithAccessAdministrationError(e);
		}
	}

	/// <summary>
	/// 处理 user delete <name> 命令，删除指定的用户，并删除与该用户相关的全部 API 凭据
	/// </summary>
	/// <param name="accessAdministrationService">访问管理服务</param>
	/// <param name="userName">要删除的用户名</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleUserDeleteAsync(IAccessAdministrationService accessAdministrationService, string userName) {
		try {
			await accessAdministrationService.DeleteUserAsync(userName);

			Console.WriteLine($"用户 {userName} 已删除");

			return 0;
		} catch (AccessAdministrationException e) {
			return ExitWithAccessAdministrationError(e);
		}
	}

	/// <summary>
	/// 打印访问管理服务业务错误，并转换为 CLI 退出代码。
	/// </summary>
	/// <param name="exception">访问管理业务异常</param>
	/// <returns>CLI 退出代码</returns>
	private static int ExitWithAccessAdministrationError(AccessAdministrationException exception) {
		Console.WriteLine(exception.Message);

		return exception.StatusCode switch {
			StatusCodes.Status409Conflict => 1,
			StatusCodes.Status404NotFound => 2,
			StatusCodes.Status400BadRequest or StatusCodes.Status422UnprocessableEntity => 3,
			_ => 6
		};
	}
}