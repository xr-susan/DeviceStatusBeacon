namespace DeviceStatusBeacon.Handlers;

/// <inheritdoc/>
public static partial class ConsoleDispatcher {
	/// <summary>
	/// 处理 account 命令
	/// </summary>
	/// <param name="argsAfterVerb">动词之后的参数</param>
	/// <param name="sp">负责依赖注入的服务提供者</param>
	/// <returns>一个表示异步操作的任务，任务结果指示应用程序的退出代码</returns>
	private static async Task<int> HandleAccountCommandAsync(string[] argsAfterVerb, IServiceProvider sp) {
		// account list
		if (argsAfterVerb is ["list"]) {
			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var accounts = await db.Accounts
				.AsNoTracking()
				.OrderBy(a => a.Username)
				.Select(a => new { a.AccountId, a.Username, a.Role })
				.Take(MaxDisplayCount + 1) // 多取1个以检测数量是否过多
				.ToListAsync();

			return PrintListWithSummary(
				accounts,
				"没有找到任何账户",
				$"账户数量过多（超过 {MaxDisplayCount} 个），请使用 query 命令或数据库管理工具查询",
				"账户",
				account => Console.WriteLine($"  [{account.AccountId}] {account.Username} ({account.Role})")
			);
		}

		// account query <part-of-name>
		if (argsAfterVerb is ["query", var partOfName]) {
			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var accounts = await db.Accounts
				.AsNoTracking()
				.Where(a => a.Username.Contains(partOfName))
				.OrderBy(a => a.Username)
				.Select(a => new { a.AccountId, a.Username, a.Role })
				.Take(MaxDisplayCount + 1) // 多取1个以检测数量是否过多
				.ToListAsync();

			return PrintListWithSummary(
				accounts,
				"没有找到匹配的账户",
				$"匹配的账户数量过多（超过 {MaxDisplayCount} 个），请使用更精确的查询条件",
				"账户",
				account => Console.WriteLine($"  [{account.AccountId}] {account.Username} ({account.Role})")
			);
		}

		// account add <name> <role>
		if (argsAfterVerb is ["add", var nameToAdd, var roleString]) {
			if (!Enum.TryParse(roleString, true, out AccountRole role)) {
				Console.WriteLine("无效的用户角色");
				return 3;
			}

			if (role == AccountRole.LimitedQuery) {
				Console.WriteLine("暂不支持使用命令行创建 LimitedQuery 角色的账户");
				return 5;
			}

			if (!GeneratedRegex.IdentityRegex().IsMatch(nameToAdd)) {
				Console.WriteLine("用户名不符合身份标识格式");
				return 3;
			}

			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var dataProtector = sp.GetRequiredService<IDataProtectorV1>();
			var unprotectedSecretKey = ISecurityServiceV1.GenerateRandomBytes();

			var newAccount = new Account {
				Username = nameToAdd,
				ProtectedSecretKey = dataProtector.ProtectKey(unprotectedSecretKey),
				Role = role
			};

			try {
				db.Accounts.Add(newAccount);
				await db.SaveChangesAsync();
			} catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 }) {
				Console.WriteLine("指定的用户名已被使用");
				return 1;
			}

			Console.WriteLine($"账户 [{newAccount.AccountId}] {newAccount.Username} 添加成功");
			Console.WriteLine($"操作密钥：{Convert.ToBase64String(unprotectedSecretKey)}");

			await UpdateLastModifiedTimeInternalAsync(db);
			return 0;
		}

		// account reset-key <name>
		if (argsAfterVerb is ["reset-key", var nameToReset]) {
			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var dataProtector = sp.GetRequiredService<IDataProtectorV1>();
			var newUnprotectedSecretKey = ISecurityServiceV1.GenerateRandomBytes();

			var updatedCount = await db.Accounts
				.Where(a => a.Username == nameToReset)
				.ExecuteUpdateAsync(a => a.SetProperty(acc => acc.ProtectedSecretKey, dataProtector.ProtectKey(newUnprotectedSecretKey)));

			if (updatedCount == 0) {
				Console.WriteLine("未找到指定的账户");
				return 2;
			}

			Console.WriteLine($"账户 {nameToReset} 的操作密钥已重置");
			Console.WriteLine($"新操作密钥：{Convert.ToBase64String(newUnprotectedSecretKey)}");

			await UpdateLastModifiedTimeInternalAsync(db);
			return 0;
		}

		// account rename <old-name> <new-name>
		if (argsAfterVerb is ["rename", var oldName, var newName]) {
			if (!GeneratedRegex.IdentityRegex().IsMatch(newName)) {
				Console.WriteLine("新用户名不符合身份标识格式");
				return 3;
			}

			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			try {
				var updatedCount = await db.Accounts
					.Where(a => a.Username == oldName)
					.ExecuteUpdateAsync(a => a.SetProperty(acc => acc.Username, newName));

				if (updatedCount == 0) {
					Console.WriteLine("未找到指定的账户");
					return 2;
				}
			} catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 }) {
				Console.WriteLine("指定的新用户名已被使用");
				return 1;
			}

			Console.WriteLine($"账户重命名成功：{oldName} -> {newName}");

			await UpdateLastModifiedTimeInternalAsync(db);
			return 0;
		}

		// account delete <name>
		if (argsAfterVerb is ["delete", var nameToDelete]) {
			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var deletedCount = await db.Accounts
				.Where(a => a.Username == nameToDelete)
				.ExecuteDeleteAsync();

			if (deletedCount == 0) {
				Console.WriteLine("未找到指定的账户");
				return 2;
			}

			Console.WriteLine($"账户 {nameToDelete} 已删除");

			await UpdateLastModifiedTimeInternalAsync(db);
			return 0;
		}

		Console.WriteLine("无效的 account 命令参数。");
		return 3;
	}
}