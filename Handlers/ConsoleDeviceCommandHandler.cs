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
		// device list
		if (argsAfterVerb is ["list"]) {
			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var devices = await db.Devices
				.AsNoTracking()
				.OrderBy(d => d.DeviceName)
				.Select(d => new { d.DeviceId, d.DeviceName, d.DisplayName })
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

		// device query <part-of-name>
		if (argsAfterVerb is ["query", var partOfName]) {
			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var devices = await db.Devices
				.AsNoTracking()
				.Where(d => d.DeviceName.Contains(partOfName))
				.OrderBy(d => d.DeviceName)
				.Select(d => new { d.DeviceId, d.DeviceName, d.DisplayName })
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

		// device show <name>
		if (argsAfterVerb is ["show", var nameToShow]) {
			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var device = await db.Devices
				.AsNoTracking()
				.Where(d => d.DeviceName == nameToShow)
				.Select(d => new { d.DeviceId, d.DeviceName, d.DisplayName, d.LatestLogTime, d.LatestReportedAddresses, d.LatestReporterRemoteAddress })
				.FirstOrDefaultAsync();

			if (device is null) {
				Console.WriteLine("未找到指定的设备");
				return 2;
			}

			Console.WriteLine("设备信息：");
			Console.WriteLine($"  设备 ID：{device.DeviceId}");
			Console.WriteLine($"  设备名称：{device.DeviceName}");
			Console.WriteLine($"  显示名称：{device.DisplayName}");
			Console.WriteLine($"  最新日志时间：{device.LatestLogTime:u}");
			Console.WriteLine($"  最新上报地址列表：[{string.Join(", ", device.LatestReportedAddresses)}]");
			Console.WriteLine($"  最新上报者远程地址：{device.LatestReporterRemoteAddress}");

			return 0;
		}

		// device history <name> <count>
		if (argsAfterVerb is ["history", var nameToGetHistory, var countString]
			&& int.TryParse(countString, out var count)) {

			if (count is <= 0 or > MaxDisplayCount) {
				Console.WriteLine($"日志数量必须在 1 到 {MaxDisplayCount} 之间");
				return 3;
			}

			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var logs = await db.OnlineLogs
				.AsNoTracking()
				.Where(l => l.Device.DeviceName == nameToGetHistory)
				.OrderByDescending(l => l.LogTime)
				.Select(l => new { l.LogTime, l.ReportedAddresses, l.ReporterRemoteAddress, l.Message })
				.Take(count)
				.ToListAsync();

			return PrintListWithHeader(
				logs,
				"未找到指定的设备或该设备没有日志",
				null,
				$"设备 {nameToGetHistory} 的最新 {logs.Count} 条日志：",
				log => {
					Console.WriteLine($"  [{log.LogTime:u}] 附加消息：{log.Message}");
					Console.WriteLine($"    上报地址列表：[{string.Join(", ", log.ReportedAddresses)}]");
					Console.WriteLine($"    上报者远程地址：{log.ReporterRemoteAddress}");
				}
			);
		}

		// device add <name> [display-name]
		if (argsAfterVerb is ["add", ..] and { Length: 2 or 3 }) {
			var nameToAdd = argsAfterVerb[1];
			var displayNameToAdd = argsAfterVerb.Length == 3 ? argsAfterVerb[2] : null;

			if (!GeneratedRegex.IdentityRegex().IsMatch(nameToAdd)) {
				Console.WriteLine("设备名称不符合身份标识格式");
				return 3;
			}

			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var dataProtector = sp.GetRequiredService<IDataProtectorV1>();
			var unprotectedSecretKey = ISecurityServiceV1.GenerateRandomBytes();

			var newDevice = new Device {
				DeviceName = nameToAdd,
				DisplayName = displayNameToAdd,
				ProtectedSecretKey = dataProtector.ProtectKey(unprotectedSecretKey)
			};

			try {
				db.Devices.Add(newDevice);
				await db.SaveChangesAsync();
			} catch (DbUpdateException e) when (e.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 }) {
				Console.WriteLine("指定的设备名称已被使用");
				return 1;
			}

			Console.WriteLine($"设备 [{newDevice.DeviceId}] {newDevice.DeviceName} 添加成功");
			Console.WriteLine($"操作密钥：{Convert.ToBase64String(unprotectedSecretKey)}");

			await UpdateLastModifiedTimeInternalAsync(db);
			return 0;
		}

		// device reset-key <name>
		if (argsAfterVerb is ["reset-key", var nameToReset]) {
			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var dataProtector = sp.GetRequiredService<IDataProtectorV1>();
			var newUnprotectedSecretKey = ISecurityServiceV1.GenerateRandomBytes();

			var updatedCount = await db.Devices
				.Where(d => d.DeviceName == nameToReset)
				.ExecuteUpdateAsync(d => d.SetProperty(dev => dev.ProtectedSecretKey, dataProtector.ProtectKey(newUnprotectedSecretKey)));

			if (updatedCount == 0) {
				Console.WriteLine("未找到指定的设备");
				return 2;
			}

			Console.WriteLine($"设备 {nameToReset} 的操作密钥已重置");
			Console.WriteLine($"新操作密钥：{Convert.ToBase64String(newUnprotectedSecretKey)}");

			await UpdateLastModifiedTimeInternalAsync(db);
			return 0;
		}

		// device rename <old-name> <new-name>
		if (argsAfterVerb is ["rename", var oldName, var newName]) {
			if (!GeneratedRegex.IdentityRegex().IsMatch(newName)) {
				Console.WriteLine("新设备名称不符合身份标识格式");
				return 3;
			}

			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

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

			await UpdateLastModifiedTimeInternalAsync(db);
			return 0;
		}

		// device set-display-name <name> <display-name>
		if (argsAfterVerb is ["set-display-name", var nameToSetDisplayName, var displayName]) {
			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var updatedCount = await db.Devices
				.Where(d => d.DeviceName == nameToSetDisplayName)
				.ExecuteUpdateAsync(d => d.SetProperty(dev => dev.DisplayName, displayName));

			if (updatedCount == 0) {
				Console.WriteLine("未找到指定的设备");
				return 2;
			}

			Console.WriteLine($"设备 {nameToSetDisplayName} 的显示名称已更新为：{displayName}");

			await UpdateLastModifiedTimeInternalAsync(db);
			return 0;
		}

		// device delete <name>
		if (argsAfterVerb is ["delete", var nameToDelete]) {
			await using var db = sp.GetRequiredService<DeviceStatusBeaconContext>();

			var deletedCount = await db.Devices
				.Where(d => d.DeviceName == nameToDelete)
				.ExecuteDeleteAsync();

			if (deletedCount == 0) {
				Console.WriteLine("未找到指定的设备");
				return 2;
			}

			Console.WriteLine($"设备 {nameToDelete} 已删除");

			await UpdateLastModifiedTimeInternalAsync(db);
			return 0;
		}


		Console.WriteLine("无效的 device 命令参数。");
		return 3;
	}
}