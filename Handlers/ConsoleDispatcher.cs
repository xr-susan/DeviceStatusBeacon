namespace DeviceStatusBeacon.Handlers;

/// <summary>
/// 控制台命令分发器
/// </summary>
public static class ConsoleDispatcher {
	private static readonly HashSet<string> ValidVerbs = ["account", "device", "help", "exit"];

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
		using var scope = services.CreateScope();
		var provider = scope.ServiceProvider;

		// 根据动词参数分发到相应的命令处理程序

	}

	private static async Task<int> HandleAccountCommandAsync(string[] args, IServiceProvider sp) {
		// account add <name> <role>
		// account list
		// account delete <name>

	}
}
