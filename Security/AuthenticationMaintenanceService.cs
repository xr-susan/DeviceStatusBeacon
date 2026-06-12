namespace DeviceStatusBeacon.Security;

/// <summary>
/// 负责执行签名鉴权相关后台维护任务的服务
/// </summary>
/// <remarks>
/// 当前统一承载的周期任务有：
/// <list type="bullet">
/// <item><description>定期清理防重放保护服务中过期的 nonce 键</description></item>
/// </list>
/// </remarks>
public sealed class AuthenticationMaintenanceService(
	INonceReplayProtectionService nonceReplayProtectionService,
	ILogger<AuthenticationMaintenanceService> logger
	) : BackgroundService {
	/// <inheritdoc/>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		// 启动 nonce 清理循环，并在应用停止时取消循环
		var nonceCleanupLoopTask = RunNonceCleanupLoopAsync(stoppingToken);

		await nonceCleanupLoopTask;
	}

	/// <summary>
	/// 运行 nonce 清理循环。
	/// </summary>
	/// <param name="nonceCleanupTimer">nonce 清理定时器</param>
	/// <param name="stoppingToken">应用停止取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	private async Task RunNonceCleanupLoopAsync(CancellationToken stoppingToken) {
		// 创建一个周期性定时器来触发 nonce 清理任务
		using PeriodicTimer nonceCleanupTimer = new(INonceReplayProtectionService.NonceCleanupInterval);

		while (await nonceCleanupTimer.WaitForNextTickAsync(stoppingToken)) {
			try {
				// nonce 清理本身不依赖数据库，直接在后台循环中同步执行即可
				nonceReplayProtectionService.CleanupExpiredNonces();
			} catch (Exception e) {
				// 捕获并记录其他可能的异常，避免因未处理的异常导致后台服务崩溃
				logger.LogError(e, "执行后台 Nonce 清理任务时发生异常");
			}
		}
	}
}