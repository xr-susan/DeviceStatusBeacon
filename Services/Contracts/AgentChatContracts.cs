using System.Text.Json;

namespace DeviceStatusBeacon.Services.Contracts;

/// <summary>
/// Agent 对话请求。
/// </summary>
/// <param name="Provider">本轮调用使用的模型供应商配置</param>
/// <param name="Messages">前端保存并回传的 Chat Completions 消息历史</param>
public sealed record AgentChatRequest(
	AgentProviderSettings Provider,
	IReadOnlyCollection<JsonElement> Messages
);

/// <summary>
/// Agent 模型供应商配置。
/// </summary>
/// <param name="Endpoint">OpenAI-compatible Chat Completions 终结点或基地址</param>
/// <param name="Model">模型名称</param>
/// <param name="ApiKey">API Key</param>
public sealed record AgentProviderSettings(
	string Endpoint,
	string Model,
	string ApiKey
);