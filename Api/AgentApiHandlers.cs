using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;

namespace DeviceStatusBeacon.Api;

/// <summary>
/// Agent 相关 Minimal API 处理逻辑。
/// </summary>
internal static class AgentApiHandlers {
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	/// <summary>
	/// 处理一轮 Agent 对话。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="agentChatService">Agent 对话服务</param>
	/// <param name="antiforgery">防伪令牌服务</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步操作的任务</returns>
	public static async Task ChatAsync(
		HttpContext context,
		IAgentChatService agentChatService,
		IAntiforgery antiforgery,
		CancellationToken cancellationToken) {
		try {
			// Agent 入口是交互式内部 API，必须和日志写入等内部接口一样校验 CSRF
			await antiforgery.ValidateRequestAsync(context);
		} catch (AntiforgeryValidationException) {
			await ApiProblemResults.InvalidAntiforgeryToken(context).ExecuteAsync(context);
			return;
		}

		// 这里使用 NDJSON 而不是普通 JSON，方便前端在模型调用和工具执行期间持续刷新状态
		context.Response.ContentType = "application/x-ndjson; charset=utf-8";
		context.Response.Headers.CacheControl = "no-store";

		try {
			// 手动读取请求体，避免 Minimal API 在进入 handler 前绑定失败并直接返回 500
			var request = await context.Request.ReadFromJsonAsync<AgentChatRequest>(JsonOptions, cancellationToken)
				?? throw new AgentChatException("Agent 请求体不能为空。");

			// 后端不保存任何会话历史，只在本次请求内注入系统提示词、工具定义并执行工具调用
			await agentChatService.ProcessAsync(
				context.User,
				request,
				async (eventType, payload, token) => await WriteEventAsync(context, eventType, payload, token),
				cancellationToken);
		} catch (AgentChatException e) {
			// 业务可预期失败以流式事件返回，让前端可以在同一套事件处理路径内展示错误
			await WriteEventAsync(context, "error", new {
				message = e.Message
			}, cancellationToken);
			await WriteEventAsync(context, "done", null, cancellationToken);
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			// 客户端主动取消请求时无需继续写响应
		} catch (Exception e) {
			// 非预期失败也尽量写成 Agent 事件，避免前端只看到一个中断的响应流
			await WriteEventAsync(context, "error", new {
				message = $"Agent 请求失败：{e.Message}"
			}, cancellationToken);
			await WriteEventAsync(context, "done", null, cancellationToken);
		}
	}

	/// <summary>
	/// 写出 NDJSON 事件。
	/// </summary>
	/// <param name="context">当前 HTTP 上下文</param>
	/// <param name="eventType">事件类型</param>
	/// <param name="payload">事件负载</param>
	/// <param name="cancellationToken">取消令牌</param>
	/// <returns>一个表示异步写入操作的任务</returns>
	private static async Task WriteEventAsync(HttpContext context, string eventType, object? payload, CancellationToken cancellationToken) {
		// 每一行都是一个完整 JSON 对象，前端按行解析即可，不需要等待整段响应完成
		var line = JsonSerializer.Serialize(new {
			type = eventType,
			payload
		}, JsonOptions);
		await context.Response.WriteAsync(line, Encoding.UTF8, cancellationToken);
		await context.Response.WriteAsync("\n", Encoding.UTF8, cancellationToken);
		// 显式 Flush 让工具开始、工具结束等事件尽快到达浏览器，改善长响应等待体验
		await context.Response.Body.FlushAsync(cancellationToken);
	}
}