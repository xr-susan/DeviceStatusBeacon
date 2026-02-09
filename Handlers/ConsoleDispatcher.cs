namespace DeviceStatusBeacon.Handlers;

/// <summary>
/// 控制台命令分发器
/// </summary>
public static partial class ConsoleDispatcher {
	internal static readonly HashSet<string> ValidVerbs = ["account", "device", "help", "exit"];
	internal const int MaxDisplayCount = 50;

	/// <summary>
	/// 根据命令行参数选择性地分发命令到控制台命令处理程序
	/// </summary>
	/// <param name="args">应用程序的命令行参数</param>
	/// <param name="services">服务提供者</param>
	/// <returns>一个表示异步操作的任务。任务结果如果为 (true, exitCode)，应当以 exitCode 退出应用程序；如果为 (false, 0)，应当继续启动 web server</returns>
	public static async Task<(bool, int)> DispatchAsync(string[] args, IServiceProvider services) {
		// 提取首个动词参数
		var verb = args.FirstOrDefault(arg => arg.Length >= 2 && arg[0] is not ('-' or '/'))?.ToLowerInvariant();

		// 验证动词参数是否有效，如果无效则返回 false，以正常启动 web server
		if (verb is null || !ValidVerbs.Contains(verb)) {
			return (false, 0);
		}

		var verbIndex = Array.FindIndex(args, arg => arg.Equals(verb, StringComparison.OrdinalIgnoreCase));
		var argsAfterVerb = args[(verbIndex + 1)..];

		using var scope = services.CreateScope();
		var provider = scope.ServiceProvider;

		// 根据动词参数分发到相应的命令处理程序
		var exitCode = verb switch {
			"account" => await HandleAccountCommandAsync(argsAfterVerb, provider),
			"device" => await HandleDeviceCommandAsync(argsAfterVerb, provider),
			"help" => HandleHelpCommand(),
			"exit" => 0,
			_ => throw new InvalidOperationException("不支持的命令动词") // 代码逻辑上不应到达此处
		};

		return (true, exitCode);
	}

	/// <summary>
	/// 打印帮助信息
	/// </summary>
	/// <returns>应用程序的退出代码，恒为 0</returns>
	private static int HandleHelpCommand() {
		Console.WriteLine("支持的命令：");
		Console.WriteLine("  account add <name> <role>                      添加新账户");
		Console.WriteLine("  account delete <name>                          删除指定账户");
		Console.WriteLine("  account list                                   列出所有账户");
		Console.WriteLine("  account query <part-of-name>                   查询匹配的账户");
		Console.WriteLine("  account rename <old-name> <new-name>           重命名指定账户");
		Console.WriteLine("  account reset-key <name>                       重置指定账户的操作密钥");
		Console.WriteLine();
		Console.WriteLine("  device add <name> [display-name]               添加新设备");
		Console.WriteLine("  device delete <name>                           删除指定设备");
		Console.WriteLine("  device history <name> <count>                  查询指定设备最近的日志");
		Console.WriteLine("  device list                                    列出所有设备");
		Console.WriteLine("  device query <part-of-name>                    查询匹配的设备");
		Console.WriteLine("  device rename <old-name> <new-name>            重命名指定设备");
		Console.WriteLine("  device reset-key <name>                        重置指定设备的操作密钥");
		Console.WriteLine("  device set-display-name <name> <display-name>  设置指定设备的显示名称");
		Console.WriteLine("  device show <name>                             展示指定设备最新的信息");
		Console.WriteLine();
		Console.WriteLine("  help                                           显示此帮助信息");
		Console.WriteLine("  exit                                           退出应用程序");

		return 0;
	}

	/// <summary>
	/// 结合自定义标题打印列表内容，并根据情况打印空列表或溢出消息
	/// </summary>
	/// <remarks>当对应消息为 null 时，不会做相应的的检查，直接视为通过；当消息为 <see cref="string.Empty"/> 时，会静默检查，如果未通过检查也不会输出任何内容，但将返回相应的退出代码</remarks>
	/// <typeparam name="T">列表项类型</typeparam>
	/// <param name="list">要打印的列表</param>
	/// <param name="emptyMessage">列表为空时显示的消息</param>
	/// <param name="overflowMessage">列表溢出时显示的消息</param>
	/// <param name="header">列表标题</param>
	/// <param name="itemPrinter">列表项打印器</param>
	/// <param name="maxDisplayCount">最大显示数量</param>
	/// <returns>应用程序的退出代码</returns>
	internal static int PrintListWithHeader<T>(List<T> list, string? emptyMessage, string? overflowMessage, string header, Action<T> itemPrinter, int maxDisplayCount = MaxDisplayCount) {
		if (emptyMessage is not null && list.Count == 0) {
			if (emptyMessage != string.Empty) {
				Console.WriteLine(emptyMessage);
			}
			return 2;
		}

		if (overflowMessage is not null && list.Count > maxDisplayCount) {
			if (overflowMessage != string.Empty) {
				Console.WriteLine(overflowMessage);
			}
			return 4;
		}

		Console.WriteLine(header);
		foreach (var item in list) {
			itemPrinter(item);
		}

		return 0;
	}

	/// <summary>
	/// 结合自定义标题格式化委托打印列表内容，并根据情况打印空列表或溢出消息
	/// </summary>
	/// <remarks>当对应消息为 null 时，不会做相应的的检查，直接视为通过；当消息为 <see cref="string.Empty"/> 时，会静默检查，如果未通过检查也不会输出任何内容，但将返回相应的退出代码</remarks>
	/// <typeparam name="T">列表项类型</typeparam>
	/// <param name="list">要打印的列表</param>
	/// <param name="emptyMessage">列表为空时显示的消息</param>
	/// <param name="overflowMessage">列表溢出时显示的消息</param>
	/// <param name="headerFormatter">列表标题格式化委托，传入参数为当前列表项数量</param>
	/// <param name="itemPrinter">列表项打印器</param>
	/// <param name="maxDisplayCount">最大显示数量</param>
	/// <returns>应用程序的退出代码</returns>
	internal static int PrintListWithHeader<T>(List<T> list, string? emptyMessage, string? overflowMessage, Func<int, string> headerFormatter, Action<T> itemPrinter, int maxDisplayCount = MaxDisplayCount) =>
		PrintListWithHeader(list, emptyMessage, overflowMessage, headerFormatter(list.Count), itemPrinter, maxDisplayCount);

	/// <summary>
	/// 结合通用标题打印列表内容，并根据情况打印空列表或溢出消息
	/// </summary>
	/// <remarks>当对应消息为 null 时，不会做相应的的检查，直接视为通过；当消息为 <see cref="string.Empty"/> 时，会静默检查，如果未通过检查也不会输出任何内容，但将返回相应的退出代码</remarks>
	/// <typeparam name="T">列表项类型</typeparam>
	/// <param name="list">要打印的列表</param>
	/// <param name="emptyMessage">列表为空时显示的消息</param>
	/// <param name="overflowMessage">列表溢出时显示的消息</param>
	/// <param name="itemTypeName">列表项类型名称（用于显示在标题中）</param>
	/// <param name="itemPrinter">列表项打印器</param>
	/// <param name="maxDisplayCount">最大显示数量</param>
	/// <returns>应用程序的退出代码</returns>
	internal static int PrintListWithSummary<T>(List<T> list, string? emptyMessage, string? overflowMessage, string itemTypeName, Action<T> itemPrinter, int maxDisplayCount = MaxDisplayCount) =>
		PrintListWithHeader(list, emptyMessage, overflowMessage, $"{itemTypeName}列表（共 {list.Count} 个{itemTypeName}）：", itemPrinter, maxDisplayCount);

	/// <summary>
	/// 更新实体认证信息最后修改时间以通知正在运行的 web server 刷新缓存
	/// </summary>
	/// <remarks>此方法不会抛出异常，在捕获到异常时将会反映到控制台</remarks>
	/// <param name="db">数据库上下文</param>
	/// <returns>一个表示异步操作的任务</returns>
	internal static async Task UpdateLastModifiedTimeInternalAsync(DeviceStatusBeaconContext db) {
		try {
			await SettingInDb.UpdateEntityAuthInfoLastModifiedTimeAsync(db);
		} catch (Exception e) {
			Console.WriteLine($"\n警告：更新实体认证信息最后修改时间时发生错误：{e.Message}");
			Console.WriteLine("基础操作已经完成，但正在运行中的 web server 可能无法及时刷新缓存以应用此更改。\n");
		}
	}
}