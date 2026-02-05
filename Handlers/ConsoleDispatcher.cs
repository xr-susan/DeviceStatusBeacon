namespace DeviceStatusBeacon.Handlers;

/// <summary>
/// 控制台命令分发器
/// </summary>
public static partial class ConsoleDispatcher {
	private static readonly HashSet<string> ValidVerbs = ["account", "device", "help", "exit"];
	private const int MaxListItems = 50;

	/// <summary>
	/// 根据命令行参数选择性地分发命令到控制台命令处理程序
	/// </summary>
	/// <param name="args">应用程序的命令行参数</param>
	/// <param name="services">服务提供者</param>
	/// <returns>如果为 (true, exitCode)，应当停止应用程序；如果为 (false, 0)，应当继续启动 web server</returns>
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

	private static int PrintList<T>(List<T> items, string emptyMessage, string tooManyMessage, string itemTypeName, Action<T> itemPrinter) {
		if (items.Count == 0) {
			Console.WriteLine(emptyMessage);
			return 2;
		}

		if (items.Count > MaxListItems) {
			Console.WriteLine(tooManyMessage);
			return 4;
		}

		Console.WriteLine($"{itemTypeName}列表（共 {items.Count} 个{itemTypeName}）：");
		foreach (var item in items) {
			itemPrinter(item);
		}

		return 0;
	}
}