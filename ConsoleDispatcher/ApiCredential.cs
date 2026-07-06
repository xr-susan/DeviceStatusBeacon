namespace DeviceStatusBeacon;

/// <inheritdoc/>
public static partial class ConsoleDispatcher {
	/// <summary>
	/// 处理 api-credential 命令
	/// </summary>
	/// <param name="argsAfterVerb">动词之后的参数</param>
	/// <param name="sp">负责依赖注入的服务提供者</param>
	/// <returns>一个表示异步操作的任务，任务结果指示应用程序的退出代码</returns>
	private static async Task<int> HandleApiCredentialCommandAsync(string[] argsAfterVerb, IServiceProvider sp) {
		var accessAdministrationQueryService = sp.GetRequiredService<IAccessAdministrationQueryService>();
		var accessAdministrationService = sp.GetRequiredService<IAccessAdministrationService>();

		return argsAfterVerb switch {
			// api-credential list <owner-user-name>
			["list", var ownerUserName] => await HandleApiCredentialListAsync(accessAdministrationQueryService, ownerUserName),

			// api-credential add <owner-user-name> <role> <display-name>
			["add", var ownerUserName, var roleString, var displayName] => await HandleApiCredentialAddAsync(accessAdministrationService, ownerUserName, roleString, displayName),

			// api-credential reset-key <api-credential-id>
			["reset-key", var credentialIdString] => await HandleApiCredentialResetKeyAsync(accessAdministrationService, credentialIdString),

			// api-credential delete <api-credential-id>
			["delete", var credentialIdString] => await HandleApiCredentialDeleteAsync(accessAdministrationService, credentialIdString),

			// 无效的 api-credential 命令参数
			_ => ExitWithInvalidCommandMessage("api-credential")
		};
	}

	/// <summary>
	/// 处理 api-credential list <owner-user-name> 命令
	/// </summary>
	/// <param name="accessAdministrationQueryService">访问管理查询服务</param>
	/// <param name="ownerUserName">要查询的所属用户名</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleApiCredentialListAsync(IAccessAdministrationQueryService accessAdministrationQueryService, string ownerUserName) {
		var apiCredentials = await accessAdministrationQueryService.GetApiCredentialsForConsoleAsync(ownerUserName);

		return PrintListWithHeader(
			apiCredentials,
			"没有找到匹配的 API 凭据",
			$"匹配的 API 凭据数量过多（超过 {MaxDisplayCount} 个），请使用更精确的查询条件",
			$"用户 {ownerUserName} 关联的 API 凭据列表（共 {apiCredentials.Count} 个 API 凭据）：",
			apiCredential => Console.WriteLine($"  [{apiCredential.ApiCredentialId}] {apiCredential.DisplayName} ({apiCredential.Role}, 用户：{apiCredential.OwnerUserName})")
		);
	}

	/// <summary>
	/// 处理 api-credential add <owner-user-name> <role> <display-name> 命令，在所属用户存在且角色边界合法时创建新的 API 凭据
	/// </summary>
	/// <param name="accessAdministrationService">访问管理服务</param>
	/// <param name="ownerUserName">所属用户名</param>
	/// <param name="roleString">要分配的角色字符串</param>
	/// <param name="displayName">API 凭据的显示名称（可选）</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleApiCredentialAddAsync(IAccessAdministrationService accessAdministrationService, string ownerUserName, string roleString, string displayName) {
		if (!PrincipalRole.TryParse(roleString, out var role)) {
			Console.WriteLine("无效的 API 凭据角色");
			return 3;
		}

		try {
			var commandResult = await accessAdministrationService.CreateApiCredentialAsync(ownerUserName, new() {
				Role = role,
				DisplayName = displayName
			});

			Console.WriteLine($"""
				API 凭据添加成功：
				  凭据 ID：{commandResult.ApiCredentialId}
				  所属用户：{ownerUserName}
				  操作密钥：{commandResult.SecretKey}
				""");

			return 0;
		} catch (AccessAdministrationException e) {
			return ExitWithAccessAdministrationError(e);
		}
	}

	/// <summary>
	/// 处理 api-credential reset-key <api-credential-id> 命令
	/// </summary>
	/// <param name="accessAdministrationService">访问管理服务</param>
	/// <param name="credentialIdString">要重置的 API 凭据 ID 字符串</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleApiCredentialResetKeyAsync(IAccessAdministrationService accessAdministrationService, string credentialIdString) {
		if (!Guid.TryParse(credentialIdString, out var credentialIdToReset)) {
			Console.WriteLine("无效的 API 凭据 ID");
			return 3;
		}

		try {
			var commandResult = await accessAdministrationService.ResetApiCredentialSecretKeyAsync(credentialIdToReset);

			Console.WriteLine($"API 凭据 [{credentialIdToReset}] 的操作密钥已重置");
			Console.WriteLine($"新操作密钥：{commandResult.SecretKey}");

			return 0;
		} catch (AccessAdministrationException e) {
			return ExitWithAccessAdministrationError(e);
		}
	}

	/// <summary>
	/// 处理 api-credential delete <api-credential-id> 命令
	/// </summary>
	/// <param name="accessAdministrationService">访问管理服务</param>
	/// <param name="credentialIdString">要删除的 API 凭据 ID 字符串</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleApiCredentialDeleteAsync(IAccessAdministrationService accessAdministrationService, string credentialIdString) {
		if (!Guid.TryParse(credentialIdString, out var credentialIdToDelete)) {
			Console.WriteLine("无效的 API 凭据 ID");
			return 3;
		}

		try {
			await accessAdministrationService.DeleteApiCredentialAsync(credentialIdToDelete);

			Console.WriteLine($"API 凭据 [{credentialIdToDelete}] 已删除");

			return 0;
		} catch (AccessAdministrationException e) {
			return ExitWithAccessAdministrationError(e);
		}
	}
}