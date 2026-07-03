using DeviceStatusBeacon.Services;

namespace DeviceStatusBeacon.Handlers;

/// <inheritdoc/>
public static partial class ConsoleDispatcher {
	/// <summary>
	/// 处理 device 命令
	/// </summary>
	/// <param name="argsAfterVerb">动词之后的参数</param>
	/// <param name="sp">负责依赖注入的服务提供者</param>
	/// <returns>一个表示异步操作的任务，任务结果指示应用程序的退出代码</returns>
	private static async Task<int> HandleDeviceCommandAsync(string[] argsAfterVerb, IServiceProvider sp) {
		var queryService = sp.GetRequiredService<IManagementQueryService>();
		var querySession = queryService.CreatePrivilegedQuerySession();
		var deviceManagementService = sp.GetRequiredService<IDeviceManagementService>();

		return argsAfterVerb switch {
			// device list
			["list"] => await HandleDeviceListAsync(queryService, querySession),

			// device query <part-of-name>
			["query", var partOfName] => await HandleDeviceQueryAsync(queryService, querySession, partOfName),

			// device show <name>
			["show", var name] => await HandleDeviceShowAsync(queryService, querySession, name),

			// device history <name> <count>
			["history", var name, var countString] => await HandleDeviceHistoryAsync(queryService, querySession, name, countString),

			// device add <name> [display-name]
			["add", var name, var displayName] => await HandleDeviceAddAsync(deviceManagementService, name, displayName),
			["add", var name] => await HandleDeviceAddAsync(deviceManagementService, name),

			// device reset-key <name>
			["reset-key", var name] => await HandleDeviceResetKeyAsync(deviceManagementService, name),

			// device rename <old-name> <new-name>
			["rename", var oldName, var newName] => await HandleDeviceRenameAsync(deviceManagementService, oldName, newName),

			// device set-display-name <name> <display-name>
			["set-display-name", var name, var displayName] => await HandleDeviceSetDisplayNameAsync(deviceManagementService, name, displayName),

			// device delete <name>
			["delete", var name] => await HandleDeviceDeleteAsync(deviceManagementService, name),

			// 无效的 device 命令参数
			_ => ExitWithInvalidCommandMessage("device")
		};
	}

	/// <summary>
	/// 处理 device list 命令
	/// </summary>
	/// <param name="queryService">共享管理查询服务</param>
	/// <param name="querySession">CLI 使用的特权查询会话</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceListAsync(IManagementQueryService queryService, ManagementQuerySession querySession) {
		var devices = await queryService.GetDeviceSliceAsync(querySession, null, MaxDisplayCount + 1, sortByNormalizedDeviceName: true);

		return PrintListWithSummary(
			devices,
			"没有找到任何设备",
			$"设备数量过多（超过 {MaxDisplayCount} 个），请使用 query 命令或数据库管理工具查询",
			"设备",
			device => Console.WriteLine($"  [{device.DeviceId}] {device.DeviceName} ({device.DisplayName ?? "<NoDisplayName>"})")
		);
	}

	/// <summary>
	/// 处理 device query <part-of-name> 命令
	/// </summary>
	/// <param name="queryService">共享管理查询服务</param>
	/// <param name="querySession">CLI 使用的特权查询会话</param>
	/// <param name="partOfName">设备名称的一部分</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceQueryAsync(IManagementQueryService queryService, ManagementQuerySession querySession, string partOfName) {
		var devices = await queryService.GetDeviceSliceAsync(querySession, partOfName, MaxDisplayCount + 1, sortByNormalizedDeviceName: true);

		return PrintListWithSummary(
			devices,
			"没有找到匹配的设备",
			$"匹配的设备数量过多（超过 {MaxDisplayCount} 个），请使用更精确的查询条件",
			"设备",
			device => Console.WriteLine($"  [{device.DeviceId}] {device.DeviceName} ({device.DisplayName ?? "<NoDisplayName>"})")
		);
	}

	/// <summary>
	/// 处理 device show <name> 命令
	/// </summary>
	/// <param name="queryService">共享管理查询服务</param>
	/// <param name="querySession">CLI 使用的特权查询会话</param>
	/// <param name="name">要展示的设备名称</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceShowAsync(IManagementQueryService queryService, ManagementQuerySession querySession, string name) {
		var device = await queryService.GetDeviceByNameAsync(querySession, name);

		if (device is null) {
			Console.WriteLine("未找到指定的设备");
			return 2;
		}

		var latestStatus = device.LatestLogTime is null
			? """
			    该设备当前暂无日志记录
			"""
			: $"""
			    上报时间：{device.LatestLogTime:u}
			    上报地址列表：[{string.Join(", ", device.LatestReportedAddresses ?? [])}]
			    上报者远程地址：{device.LatestReporterRemoteAddress}
			""";

		Console.WriteLine($"""
			设备信息：
			  设备 ID：{device.DeviceId}
			  设备名称：{device.DeviceName}
			  显示名称：{device.DisplayName}
			  最新状态：
			    设备状态：{(device.Enabled ? "启用" : "停用")}
			{latestStatus}
			""");

		return 0;
	}

	/// <summary>
	/// 处理 device history <name> <count> 命令
	/// </summary>
	/// <param name="queryService">共享管理查询服务</param>
	/// <param name="querySession">CLI 使用的特权查询会话</param>
	/// <param name="name">要查询日志的设备名称</param>
	/// <param name="countString">要查询的日志数量字符串</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceHistoryAsync(IManagementQueryService queryService, ManagementQuerySession querySession, string name, string countString) {
		if (!int.TryParse(countString, out var count) || count is <= 0 or > MaxDisplayCount) {
			Console.WriteLine($"日志数量必须在 1 到 {MaxDisplayCount} 之间");
			return 3;
		}

		var logs = await queryService.GetLogsByDeviceNameAsync(querySession, name, count);

		return PrintListWithHeader(
			logs,
			"未找到指定的设备或该设备没有日志",
			null,
			$"设备 {name} 的最新 {logs.Count} 条日志：",
			log => {
				Console.WriteLine($"  [{log.LogTime:u}] 附加消息：{log.Message}");
				Console.WriteLine($"    上报地址列表：[{string.Join(", ", log.ReportedAddresses ?? [])}]");
				Console.WriteLine($"    上报者远程地址：{log.ReporterRemoteAddress}");
			}
		);
	}

	/// <summary>
	/// 处理 device add <name> [display-name] 命令
	/// </summary>
	/// <param name="deviceManagementService">设备管理服务</param>
	/// <param name="name">设备名称</param>
	/// <param name="displayName">设备显示名称（可选）</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceAddAsync(IDeviceManagementService deviceManagementService, string name, string? displayName = null) {
		try {
			var commandResult = await deviceManagementService.CreateAsync(new() {
				DeviceName = name,
				DisplayName = displayName
			});

			Console.WriteLine($"""
				设备添加成功：
				  设备 ID：{commandResult.DeviceId}
				  设备名称：{commandResult.DeviceName}
				  显示名称：{commandResult.DisplayName}
				  操作密钥：{commandResult.SecretKey}
				""");

			return 0;
		} catch (DeviceManagementCommandException e) {
			return ExitWithDeviceManagementError(e);
		}
	}

	/// <summary>
	/// 处理 device reset-key <name> 命令
	/// </summary>
	/// <param name="deviceManagementService">设备管理服务</param>
	/// <param name="name">要重置操作密钥的设备名称</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceResetKeyAsync(IDeviceManagementService deviceManagementService, string name) {
		try {
			var commandResult = await deviceManagementService.ResetSecretKeyAsync(name);

			Console.WriteLine($"设备 {name} 的操作密钥已重置");
			Console.WriteLine($"新操作密钥：{commandResult.SecretKey}");

			return 0;
		} catch (DeviceManagementCommandException e) {
			return ExitWithDeviceManagementError(e);
		}
	}

	/// <summary>
	/// 处理 device rename <old-name> <new-name> 命令
	/// </summary>
	/// <param name="deviceManagementService">设备管理服务</param>
	/// <param name="oldName">旧设备名称</param>
	/// <param name="newName">新设备名称</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceRenameAsync(IDeviceManagementService deviceManagementService, string oldName, string newName) {
		try {
			await deviceManagementService.RenameAsync(oldName, new() {
				NewDeviceName = newName
			});

			Console.WriteLine($"设备重命名成功：{oldName} -> {newName}");

			return 0;
		} catch (DeviceManagementCommandException e) {
			return ExitWithDeviceManagementError(e);
		}
	}

	/// <summary>
	/// 处理 device set-display-name <name> <display-name> 命令
	/// </summary>
	/// <param name="deviceManagementService">设备管理服务</param>
	/// <param name="name">要设置显示名称的设备名称</param>
	/// <param name="displayName">新的显示名称</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceSetDisplayNameAsync(IDeviceManagementService deviceManagementService, string name, string displayName) {
		try {
			await deviceManagementService.SetDisplayNameAsync(name, new() {
				DisplayName = displayName
			});

			Console.WriteLine($"设备 {name} 的显示名称已更新为：{displayName}");

			return 0;
		} catch (DeviceManagementCommandException e) {
			return ExitWithDeviceManagementError(e);
		}
	}

	/// <summary>
	/// 处理 device delete <name> 命令
	/// </summary>
	/// <param name="deviceManagementService">设备管理服务</param>
	/// <param name="name">要删除的设备名称</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceDeleteAsync(IDeviceManagementService deviceManagementService, string name) {
		try {
			await deviceManagementService.DeleteAsync(name);

			Console.WriteLine($"设备 {name} 已删除");

			return 0;
		} catch (DeviceManagementCommandException e) {
			return ExitWithDeviceManagementError(e);
		}
	}

	/// <summary>
	/// 打印设备管理服务业务错误，并转换为 CLI 退出代码。
	/// </summary>
	/// <param name="exception">设备管理业务异常</param>
	/// <returns>CLI 退出代码</returns>
	private static int ExitWithDeviceManagementError(DeviceManagementCommandException exception) {
		Console.WriteLine(exception.Message);

		return exception.StatusCode switch {
			StatusCodes.Status409Conflict => 1,
			StatusCodes.Status404NotFound => 2,
			StatusCodes.Status400BadRequest or StatusCodes.Status422UnprocessableEntity => 3,
			_ => 6
		};
	}
}