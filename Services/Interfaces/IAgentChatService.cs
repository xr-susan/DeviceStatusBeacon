using System.Security.Claims;

namespace DeviceStatusBeacon.Services.Interfaces;

/// <summary>
/// Agent 对话服务。
/// </summary>
public interface IAgentChatService {
	/// <summary>
	/// 处理一轮无状态 Agent 对话。
	/// </summary>
	/// <param name="principal">当前登录主体</param>
	/// <param name="request">Agent 对话请求</param>
	/// <param name="writeEventAsync">写出流式事件的回调</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	Task ProcessAsync(
		ClaimsPrincipal principal,
		AgentChatRequest request,
		Func<string, object?, CancellationToken, Task> writeEventAsync,
		CancellationToken cancellationToken = default);
}