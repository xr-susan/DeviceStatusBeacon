using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

namespace DeviceStatusBeacon.Handlers;

/// <inheritdoc/>
public static partial class ConsoleDispatcher {
	/// <summary>
	/// 处理 api-credential 命令
	/// </summary>
	/// <param name="argsAfterVerb">动词之后的参数</param>
	/// <param name="sp">负责依赖注入的服务提供者</param>
	/// <returns>一个表示异步操作的任务，任务结果指示应用程序的退出代码</returns>
	private static async Task<int> HandleApiCredentialCommandAsync(string[] argsAfterVerb, IServiceProvider sp) {
		await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

		return argsAfterVerb switch {
			// api-credential list
			["list"] => await HandleApiCredentialListAsync(db),

			// api-credential query-by-user <user-name>
			["query-by-user", var ownerUserName] => await HandleApiCredentialQueryByUserAsync(db, sp, ownerUserName),

			// api-credential add <owner-user-name> <role> [display-name]
			["add", var ownerUserName, var roleString, var displayName] => await HandleApiCredentialAddAsync(db, sp, ownerUserName, roleString, displayName),
			["add", var ownerUserName, var roleString] => await HandleApiCredentialAddAsync(db, sp, ownerUserName, roleString),

			// api-credential reset-key <api-credential-id>
			["reset-key", var credentialIdString] => await HandleApiCredentialResetKeyAsync(db, sp, credentialIdString),

			// api-credential delete <api-credential-id>
			["delete", var credentialIdString] => await HandleApiCredentialDeleteAsync(db, credentialIdString),

			// 无效的 api-credential 命令参数
			_ => ExitWithInvalidCommandMessage("api-credential")
		};
	}

	/// <summary>
	/// 处理 api-credential list 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleApiCredentialListAsync(DeviceStatusBeaconContext db) {
		var apiCredentials = await db.ApiCredentials
			.AsNoTracking()
			.Select(c => new {
				c.ApiCredentialId,
				c.DisplayName,
				c.Role,
				OwnerUserName = c.User.UserName
			})
			.OrderBy(c => c.OwnerUserName)
			.ThenBy(c => c.DisplayName)
			.Take(MaxDisplayCount + 1)
			.ToListAsync();

		return PrintListWithSummary(
			apiCredentials,
			"没有找到任何 API 凭据",
			$"API 凭据数量过多（超过 {MaxDisplayCount} 个），请使用 query-by-user 命令或数据库管理工具查询",
			"API 凭据",
			apiCredential => Console.WriteLine($"  [{apiCredential.ApiCredentialId}] {apiCredential.DisplayName ?? "<NoDisplayName>"} ({apiCredential.Role}, 用户：{apiCredential.OwnerUserName})")
		);
	}

	/// <summary>
	/// 处理 api-credential query-by-user <user-name> 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="sp">负责依赖注入的服务提供者</param>
	/// <param name="ownerUserName">要查询的所属用户名</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleApiCredentialQueryByUserAsync(DeviceStatusBeaconContext db, IServiceProvider sp, string ownerUserName) {
		var normalizer = sp.GetRequiredService<ILookupNormalizer>();
		var normalizedOwnerUserName = normalizer.NormalizeName(ownerUserName);

		var apiCredentials = await db.ApiCredentials
			.AsNoTracking()
			.Where(c => c.User.NormalizedUserName == normalizedOwnerUserName)
			.Select(c => new {
				c.ApiCredentialId,
				c.DisplayName,
				c.Role,
				OwnerUserName = c.User.UserName
			})
			.OrderBy(c => c.DisplayName)
			.Take(MaxDisplayCount + 1)
			.ToListAsync();

		return PrintListWithHeader(
			apiCredentials,
			"没有找到匹配的 API 凭据",
			$"匹配的 API 凭据数量过多（超过 {MaxDisplayCount} 个），请使用更精确的查询条件",
			$"用户 {ownerUserName} 关联的 API 凭据列表（共 {apiCredentials.Count} 个 API 凭据）：",
			apiCredential => Console.WriteLine($"  [{apiCredential.ApiCredentialId}] {apiCredential.DisplayName ?? "<NoDisplayName>"} ({apiCredential.Role}, 用户：{apiCredential.OwnerUserName})")
		);
	}

	/// <summary>
	/// 处理 api-credential add <owner-user-name> <role> [display-name] 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="sp">负责依赖注入的服务提供者</param>
	/// <param name="ownerUserName">所属用户名</param>
	/// <param name="roleString">要分配的角色字符串</param>
	/// <param name="displayName">API 凭据的显示名称（可选）</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleApiCredentialAddAsync(DeviceStatusBeaconContext db, IServiceProvider sp, string ownerUserName, string roleString, string? displayName = null) {
		if (!Enum.TryParse(roleString, true, out PrincipalRole role)) {
			Console.WriteLine("无效的 API 凭据角色");
			return 3;
		}

		if (role == PrincipalRole.LimitedQuery) {
			// 由于 LimitedQuery 涉及到复杂的列表，且当前命令行工具的功能定位更偏向于简单的管理
			// 因此计划仅通过 Web / API 支持 LimitedQuery 角色的 API 凭据创建
			Console.WriteLine("暂不支持使用命令行创建 LimitedQuery 角色的 API 凭据");
			return 5;
		}

		if (!GeneratedRegex.IdentityRegex().IsMatch(ownerUserName)) {
			Console.WriteLine("所属用户名不符合身份标识格式");
			return 3;
		}

		var normalizer = sp.GetRequiredService<ILookupNormalizer>();
		var normalizedOwnerUserName = normalizer.NormalizeName(ownerUserName);

		var owner = await db.Users
			.AsNoTracking()
			.Where(u => u.NormalizedUserName == normalizedOwnerUserName)
			.Select(u => new {
				u.Id,
				RoleName = u.UserRoles.Select(ur => ur.Role.Name).SingleOrDefault()
			})
			.SingleOrDefaultAsync();

		if (owner is null) {
			Console.WriteLine("未找到指定的所属用户");
			return 2;
		}

		if (!Enum.TryParse(owner.RoleName, true, out PrincipalRole ownerRole)) {
			Console.WriteLine("所属用户未正确设置角色");
			return 6;
		}

		if (role > ownerRole) {
			Console.WriteLine("API 凭据角色不得高于所属用户角色");
			return 3;
		}

		var dataProtector = sp.GetRequiredService<IDataProtectorV1>();
		var unprotectedSecretKey = ISecurityServiceV1.GenerateRandomBytes();

		var newCredential = new ApiCredential {
			UserId = owner.Id,
			ProtectedSecretKey = dataProtector.ProtectKey(unprotectedSecretKey),
			Role = role
		};

		if (!string.IsNullOrWhiteSpace(displayName)) {
			newCredential.DisplayName = displayName;
		}

		try {
			db.ApiCredentials.Add(newCredential);
			await db.SaveChangesAsync();
		} catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 }) {
			// 当前代码逻辑下，如果出现 SQLite 错误码 2067 (UNIQUE constraint failed)
			// 几乎只可能是由于 DisplayName 在同一关联用户下重复导致的
			Console.WriteLine("指定的 API 凭据显示名称在关联用户下已被使用");
			return 1;
		}

		Console.WriteLine($"""
			API 凭据添加成功：
			  凭据 ID：{newCredential.ApiCredentialId}
			  所属用户：{ownerUserName}
			  操作密钥：{Convert.ToBase64String(unprotectedSecretKey)}
			""");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}

	/// <summary>
	/// 处理 api-credential reset-key <api-credential-id> 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="sp">负责依赖注入的服务提供者</param>
	/// <param name="credentialIdString">要重置的 API 凭据 ID 字符串</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleApiCredentialResetKeyAsync(DeviceStatusBeaconContext db, IServiceProvider sp, string credentialIdString) {
		if (!Guid.TryParse(credentialIdString, out var credentialIdToReset)) {
			Console.WriteLine("无效的 API 凭据 ID");
			return 3;
		}

		var dataProtector = sp.GetRequiredService<IDataProtectorV1>();
		var unprotectedSecretKey = ISecurityServiceV1.GenerateRandomBytes();

		var updatedCount = await db.ApiCredentials
			.Where(c => c.ApiCredentialId == credentialIdToReset)
			.ExecuteUpdateAsync(c => c.SetProperty(credential => credential.ProtectedSecretKey, dataProtector.ProtectKey(unprotectedSecretKey)));

		if (updatedCount == 0) {
			Console.WriteLine("未找到指定的 API 凭据");
			return 2;
		}

		Console.WriteLine($"API 凭据 [{credentialIdToReset}] 的操作密钥已重置");
		Console.WriteLine($"新操作密钥：{Convert.ToBase64String(unprotectedSecretKey)}");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}

	/// <summary>
	/// 处理 api-credential delete <api-credential-id> 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="credentialIdString">要删除的 API 凭据 ID 字符串</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleApiCredentialDeleteAsync(DeviceStatusBeaconContext db, string credentialIdString) {
		if (!Guid.TryParse(credentialIdString, out var credentialIdToDelete)) {
			Console.WriteLine("无效的 API 凭据 ID");
			return 3;
		}

		var deletedCount = await db.ApiCredentials
			.Where(c => c.ApiCredentialId == credentialIdToDelete)
			.ExecuteDeleteAsync();

		if (deletedCount == 0) {
			Console.WriteLine("未找到指定的 API 凭据");
			return 2;
		}

		Console.WriteLine($"API 凭据 [{credentialIdToDelete}] 已删除");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}
}