namespace DeviceStatusBeacon;

/// <summary>
/// 控制台命令分发器
/// </summary>
/// <remarks>
/// 该分发器只在识别到受支持的控制台命令时接管程序启动流程；
/// 否则应用会继续按常规方式启动 Web 服务。
/// </remarks>
public static partial class ConsoleDispatcher {
	internal static readonly HashSet<string> ValidVerbs = ["api-credential", "device", "user", "help", "exit"];
	private const int MaxDisplayCount = AccessAdministrationQueryService.MaxAccessQueryCount;

	/// <summary>
	/// 根据命令行参数选择性地分发命令到控制台命令处理程序
	/// </summary>
	/// <param name="args">应用程序的命令行参数</param>
	/// <param name="services">服务提供者</param>
	/// <returns>一个表示异步操作的任务。任务结果如果为 <c>(true, exitCode)</c>，应当以对应退出代码结束进程；如果为 <c>(false, 0)</c>，说明当前参数不构成受支持的控制台命令，应继续启动 Web 服务</returns>
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
			"api-credential" => await HandleApiCredentialCommandAsync(argsAfterVerb, provider),
			"device" => await HandleDeviceCommandAsync(argsAfterVerb, provider),
			"user" => await HandleUserCommandAsync(argsAfterVerb, provider),
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
		Console.WriteLine(
			"""
			支持的命令：
			  api-credential add <user-name> <role> <display-name>  添加新 API 凭据
			  api-credential delete <api-credential-id>             删除指定 API 凭据
			  api-credential list <user-name>                       列出指定用户关联的 API 凭据
			  api-credential reset-key <api-credential-id>          重置指定 API 凭据的操作密钥

			  user add <name> <role> <password>                     添加新用户
			  user delete <name>                                    删除指定用户
			  user list                                             列出所有用户
			  user query <part-of-name>                             查询匹配的用户
			  user rename <old-name> <new-name>                     重命名指定用户
			  user reset-password <name> <new-password>             重置指定用户的密码
			  user set-role <name> <role>                           设置指定用户的角色

			  device add <name> [display-name]                      添加新设备
			  device delete <name>                                  删除指定设备
			  device history <name> <count>                         查询指定设备最近的日志
			  device list                                           列出所有设备
			  device query <part-of-name>                           查询匹配的设备
			  device rename <old-name> <new-name>                   重命名指定设备
			  device reset-key <name>                               重置指定设备的操作密钥
			  device set-display-name <name> <display-name>         设置指定设备的显示名称
			  device show <name>                                    展示指定设备最新的信息

			  help                                                  显示此帮助信息
			  exit                                                  退出应用程序
			""");

		return 0;
	}

	/// <summary>
	/// 当控制台命令参数无效时，打印相应的错误消息并返回一个固定的退出代码
	/// </summary>
	/// <param name="commandCategory">命令类别</param>
	/// <returns>应用程序的退出代码，固定为 3</returns>
	internal static int ExitWithInvalidCommandMessage(string commandCategory) {
		Console.WriteLine($"无效的 {commandCategory} 命令参数。");
		Console.WriteLine("使用 'help' 命令查看支持的命令列表。");
		return 3;
	}

	/// <summary>
	/// 结合自定义标题打印集合内容，并根据情况打印空集合或溢出消息
	/// </summary>
	/// <remarks>
	/// 该方法依赖 <see cref="IReadOnlyCollection{T}.Count"/> 判断空集合和溢出。
	/// 当对应消息为 null 时，不做相应检查，直接视为通过；
	/// 当消息为 <see cref="string.Empty"/> 时，会静默检查，如果未通过检查也不会输出任何内容，但仍会返回相应的退出代码。
	/// </remarks>
	/// <typeparam name="T">集合项类型</typeparam>
	/// <param name="collection">要打印的只读集合</param>
	/// <param name="emptyMessage">集合为空时显示的消息</param>
	/// <param name="overflowMessage">集合溢出时显示的消息</param>
	/// <param name="header">集合标题</param>
	/// <param name="itemPrinter">集合项打印器</param>
	/// <param name="maxDisplayCount">最大显示数量</param>
	/// <returns>应用程序的退出代码</returns>
	internal static int PrintListWithHeader<T>(IReadOnlyCollection<T> collection, string? emptyMessage, string? overflowMessage, string header, Action<T> itemPrinter, int maxDisplayCount = MaxDisplayCount) {
		if (emptyMessage is not null && collection.Count == 0) {
			if (emptyMessage != string.Empty) {
				Console.WriteLine(emptyMessage);
			}
			return 2;
		}

		if (overflowMessage is not null && collection.Count > maxDisplayCount) {
			if (overflowMessage != string.Empty) {
				Console.WriteLine(overflowMessage);
			}
			return 4;
		}

		Console.WriteLine(header);
		foreach (var item in collection) {
			itemPrinter(item);
		}

		return 0;
	}

	/// <summary>
	/// 结合自定义标题格式化委托打印集合内容，并根据情况打印空集合或溢出消息
	/// </summary>
	/// <remarks>
	/// 当对应消息为 null 时，不做相应检查，直接视为通过；
	/// 当消息为 <see cref="string.Empty"/> 时，会静默检查，如果未通过检查也不会输出任何内容，但仍会返回相应的退出代码。
	/// </remarks>
	/// <typeparam name="T">集合项类型</typeparam>
	/// <param name="collection">要打印的只读集合</param>
	/// <param name="emptyMessage">集合为空时显示的消息</param>
	/// <param name="overflowMessage">集合溢出时显示的消息</param>
	/// <param name="headerFormatter">集合标题格式化委托，传入参数为当前集合项数量</param>
	/// <param name="itemPrinter">集合项打印器</param>
	/// <param name="maxDisplayCount">最大显示数量</param>
	/// <returns>应用程序的退出代码</returns>
	internal static int PrintListWithHeader<T>(IReadOnlyCollection<T> collection, string? emptyMessage, string? overflowMessage, Func<int, string> headerFormatter, Action<T> itemPrinter, int maxDisplayCount = MaxDisplayCount) =>
		PrintListWithHeader(collection, emptyMessage, overflowMessage, headerFormatter(collection.Count), itemPrinter, maxDisplayCount);

	/// <summary>
	/// 结合通用标题打印集合内容，并根据情况打印空集合或溢出消息
	/// </summary>
	/// <remarks>
	/// 当对应消息为 null 时，不做相应检查，直接视为通过；
	/// 当消息为 <see cref="string.Empty"/> 时，会静默检查，如果未通过检查也不会输出任何内容，但仍会返回相应的退出代码。
	/// </remarks>
	/// <typeparam name="T">集合项类型</typeparam>
	/// <param name="collection">要打印的只读集合</param>
	/// <param name="emptyMessage">集合为空时显示的消息</param>
	/// <param name="overflowMessage">集合溢出时显示的消息</param>
	/// <param name="itemTypeName">集合项类型名称（用于显示在标题中）</param>
	/// <param name="itemPrinter">集合项打印器</param>
	/// <param name="maxDisplayCount">最大显示数量</param>
	/// <returns>应用程序的退出代码</returns>
	internal static int PrintListWithSummary<T>(IReadOnlyCollection<T> collection, string? emptyMessage, string? overflowMessage, string itemTypeName, Action<T> itemPrinter, int maxDisplayCount = MaxDisplayCount) =>
		PrintListWithHeader(collection, emptyMessage, overflowMessage, $"{itemTypeName}列表（共 {collection.Count} 个{itemTypeName}）：", itemPrinter, maxDisplayCount);
}