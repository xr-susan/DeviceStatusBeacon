using Microsoft.Data.Sqlite;

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
		await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

		return argsAfterVerb switch {
			// device list
			["list"] => await HandleDeviceListAsync(db),

			// device query <part-of-name>
			["query", var partOfName] => await HandleDeviceQueryAsync(db, partOfName),

			// device show <name>
			["show", var name] => await HandleDeviceShowAsync(db, name),

			// device history <name> <count>
			["history", var name, var countString] => await HandleDeviceHistoryAsync(db, name, countString),

			// device add <name> [display-name]
			["add", var name, var displayName] => await HandleDeviceAddAsync(db, sp, name, displayName),
			["add", var name] => await HandleDeviceAddAsync(db, sp, name),

			// device reset-key <name>
			["reset-key", var name] => await HandleDeviceResetKeyAsync(db, sp, name),

			// device rename <old-name> <new-name>
			["rename", var oldName, var newName] => await HandleDeviceRenameAsync(db, oldName, newName),

			// device set-display-name <name> <display-name>
			["set-display-name", var name, var displayName] => await HandleDeviceSetDisplayNameAsync(db, name, displayName),

			// device delete <name>
			["delete", var name] => await HandleDeviceDeleteAsync(db, name),

			// 无效的 device 命令参数
			_ => ExitWithInvalidCommandMessage("device")
		};
	}

	/// <summary>
	/// 处理 device list 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceListAsync(DeviceStatusBeaconContext db) {
		var devices = await db.Devices
			.AsNoTracking()
			.Select(d => new { d.DeviceId, d.DeviceName, d.DisplayName })
			.OrderBy(d => d.DeviceName)
			.Take(MaxDisplayCount + 1) // 多取1个以检测数量是否过多
			.ToListAsync();

		return PrintListWithSummary(
			devices,
			"没有找到任何设备",
			$"设备数量过多（超过 {MaxDisplayCount} 个），请使用 query 命令或数据库管理工具查询",
			"设备",
			device => Console.WriteLine($"  [{device.DeviceId}] {device.DeviceName} ({device.DisplayName})")
		);
	}

	/// <summary>
	/// 处理 device query <part-of-name> 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="partOfName">设备名称的一部分</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceQueryAsync(DeviceStatusBeaconContext db, string partOfName) {
		var devices = await db.Devices
			.AsNoTracking()
			.Where(d => d.DeviceName.Contains(partOfName))
			.Select(d => new { d.DeviceId, d.DeviceName, d.DisplayName })
			.OrderBy(d => d.DeviceName)
			.Take(MaxDisplayCount + 1) // 多取1个以检测数量是否过多
			.ToListAsync();

		return PrintListWithSummary(
			devices,
			"没有找到匹配的设备",
			$"匹配的设备数量过多（超过 {MaxDisplayCount} 个），请使用更精确的查询条件",
			"设备",
			device => Console.WriteLine($"  [{device.DeviceId}] {device.DeviceName} ({device.DisplayName})")
		);
	}

	/// <summary>
	/// 处理 device show <name> 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="name">要展示的设备名称</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceShowAsync(DeviceStatusBeaconContext db, string name) {
		var device = await db.Devices
			.AsNoTracking()
			.Where(d => d.DeviceName == name)
			.Select(d => new { d.DeviceId, d.DeviceName, d.DisplayName, d.LatestLogTime, d.LatestReportedAddresses, d.LatestReporterRemoteAddress })
			.SingleOrDefaultAsync();

		if (device is null) {
			Console.WriteLine("未找到指定的设备");
			return 2;
		}

		Console.WriteLine($"""
		设备信息：
		  设备 ID：{device.DeviceId}
		  设备名称：{device.DeviceName}
		  显示名称：{device.DisplayName}
		  最新日志时间：{device.LatestLogTime:u}
		  最新上报地址列表：[{string.Join(", ", device.LatestReportedAddresses)}]
		  最新上报者远程地址：{device.LatestReporterRemoteAddress}
		""");

		return 0;
	}

	/// <summary>
	/// 处理 device history <name> <count> 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="name">要查询日志的设备名称</param>
	/// <param name="countString">要查询的日志数量字符串</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceHistoryAsync(DeviceStatusBeaconContext db, string name, string countString) {
		if (!int.TryParse(countString, out var count) || count is <= 0 or > MaxDisplayCount) {
			Console.WriteLine($"日志数量必须在 1 到 {MaxDisplayCount} 之间");
			return 3;
		}

		// 使用子查询获取设备 ID，通过索引提高性能
		var logs = await db.OnlineLogs
			.AsNoTracking()
			.Where(l => l.DeviceId == db.Devices
				.Where(d => d.DeviceName == name)
				.Select(d => d.DeviceId)
				.SingleOrDefault())
			.Select(l => new { l.LogTime, l.ReportedAddresses, l.ReporterRemoteAddress, l.Message })
			.OrderByDescending(l => l.LogTime)
			.Take(count)
			.ToListAsync();

		return PrintListWithHeader(
			logs,
			"未找到指定的设备或该设备没有日志",
			null,
			$"设备 {name} 的最新 {logs.Count} 条日志：",
			log => {
				Console.WriteLine($"  [{log.LogTime:u}] 附加消息：{log.Message}");
				Console.WriteLine($"    上报地址列表：[{string.Join(", ", log.ReportedAddresses)}]");
				Console.WriteLine($"    上报者远程地址：{log.ReporterRemoteAddress}");
			}
		);
	}

	/// <summary>
	/// 处理 device add <name> [display-name] 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="sp">负责依赖注入的服务提供者</param>
	/// <param name="name">设备名称</param>
	/// <param name="displayName">设备显示名称（可选）</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceAddAsync(DeviceStatusBeaconContext db, IServiceProvider sp, string name, string? displayName = null) {
		if (!GeneratedRegex.IdentityRegex().IsMatch(name)) {
			Console.WriteLine("设备名称不符合身份标识格式");
			return 3;
		}

		var dataProtector = sp.GetRequiredService<IDataProtectorV1>();
		var unprotectedSecretKey = ISecurityServiceV1.GenerateRandomBytes();

		var newDevice = new Device {
			DeviceName = name,
			DisplayName = displayName,
			ProtectedSecretKey = dataProtector.ProtectKey(unprotectedSecretKey)
		};

		try {
			db.Devices.Add(newDevice);
			await db.SaveChangesAsync();
		} catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 }) {
			Console.WriteLine("指定的设备名称已被使用");
			return 1;
		}

		Console.WriteLine($"""
			设备添加成功：
			  设备 ID：{newDevice.DeviceId}
			  设备名称：{newDevice.DeviceName}
			  显示名称：{newDevice.DisplayName}
			  操作密钥：{Convert.ToBase64String(unprotectedSecretKey)}
			""");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}

	/// <summary>
	/// 处理 device reset-key <name> 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="sp">负责依赖注入的服务提供者</param>
	/// <param name="name">要重置操作密钥的设备名称</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceResetKeyAsync(DeviceStatusBeaconContext db, IServiceProvider sp, string name) {
		var dataProtector = sp.GetRequiredService<IDataProtectorV1>();
		var unprotectedSecretKey = ISecurityServiceV1.GenerateRandomBytes();

		var updatedCount = await db.Devices
			.Where(d => d.DeviceName == name)
			.ExecuteUpdateAsync(d => d.SetProperty(dev => dev.ProtectedSecretKey, dataProtector.ProtectKey(unprotectedSecretKey)));

		if (updatedCount == 0) {
			Console.WriteLine("未找到指定的设备");
			return 2;
		}

		Console.WriteLine($"设备 {name} 的操作密钥已重置");
		Console.WriteLine($"新操作密钥：{Convert.ToBase64String(unprotectedSecretKey)}");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}

	/// <summary>
	/// 处理 device rename <old-name> <new-name> 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="oldName">旧设备名称</param>
	/// <param name="newName">新设备名称</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceRenameAsync(DeviceStatusBeaconContext db, string oldName, string newName) {
		if (!GeneratedRegex.IdentityRegex().IsMatch(newName)) {
			Console.WriteLine("新设备名称不符合身份标识格式");
			return 3;
		}

		try {
			var updatedCount = await db.Devices
				.Where(d => d.DeviceName == oldName)
				.ExecuteUpdateAsync(d => d.SetProperty(dev => dev.DeviceName, newName));

			if (updatedCount == 0) {
				Console.WriteLine("未找到指定的设备");
				return 2;
			}
		} catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 }) {
			Console.WriteLine("指定的新设备名称已被使用");
			return 1;
		}

		Console.WriteLine($"设备重命名成功：{oldName} -> {newName}");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}

	/// <summary>
	/// 处理 device set-display-name <name> <display-name> 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="name">要设置显示名称的设备名称</param>
	/// <param name="displayName">新的显示名称</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceSetDisplayNameAsync(DeviceStatusBeaconContext db, string name, string displayName) {
		var updatedCount = await db.Devices
			.Where(d => d.DeviceName == name)
			.ExecuteUpdateAsync(d => d.SetProperty(dev => dev.DisplayName, displayName));

		if (updatedCount == 0) {
			Console.WriteLine("未找到指定的设备");
			return 2;
		}

		Console.WriteLine($"设备 {name} 的显示名称已更新为：{displayName}");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}

	/// <summary>
	/// 处理 device delete <name> 命令
	/// </summary>
	/// <param name="db">数据库上下文</param>
	/// <param name="name">要删除的设备名称</param>
	/// <returns>一个表示异步操作的任务，返回操作结果的状态码</returns>
	private static async Task<int> HandleDeviceDeleteAsync(DeviceStatusBeaconContext db, string name) {
		var deletedCount = await db.Devices
			.Where(d => d.DeviceName == name)
			.ExecuteDeleteAsync();

		if (deletedCount == 0) {
			Console.WriteLine("未找到指定的设备");
			return 2;
		}

		Console.WriteLine($"设备 {name} 已删除");

		await UpdateEntityAuthInfoVersionInternalAsync(db);
		return 0;
	}
}