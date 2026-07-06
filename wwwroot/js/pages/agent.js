const maxSessionCount = 5;
const maxRoundCount = 5;
const sessionsKey = "deviceStatusBeacon.agent.sessions.v1";
const activeSessionKey = "deviceStatusBeacon.agent.activeSession.v1";
const endpointKey = "deviceStatusBeacon.agent.endpoint.v1";
const modelKey = "deviceStatusBeacon.agent.model.v1";
const apiKeyStorageKey = "deviceStatusBeacon.agent.apiKey.v1";

const root = document.querySelector("[data-agent-root]");

if (root instanceof HTMLElement) {
    initializeAgent(root);
}

function initializeAgent(root) {
    const apiUrl = root.dataset.agentApiUrl;
    const requestVerificationToken = root.dataset.requestVerificationToken;
    const sessionList = root.querySelector("[data-agent-session-list]");
    const chat = root.querySelector("[data-agent-chat]");
    const composer = root.querySelector("[data-agent-composer]");
    const statusElement = root.querySelector("[data-agent-status]");
    const submitButton = root.querySelector("[data-agent-submit]");
    const newSessionButton = root.querySelector("[data-agent-new-session]");
    const endpointInput = root.querySelector("#agent-endpoint");
    const modelInput = root.querySelector("#agent-model");
    const apiKeyInput = root.querySelector("#agent-api-key");
    const messageInput = root.querySelector("#agent-message");

    if (!apiUrl
        || !(sessionList instanceof HTMLElement)
        || !(chat instanceof HTMLElement)
        || !(composer instanceof HTMLFormElement)
        || !(statusElement instanceof HTMLElement)
        || !(submitButton instanceof HTMLButtonElement)
        || !(newSessionButton instanceof HTMLButtonElement)
        || !(endpointInput instanceof HTMLInputElement)
        || !(modelInput instanceof HTMLInputElement)
        || !(apiKeyInput instanceof HTMLInputElement)
        || !(messageInput instanceof HTMLTextAreaElement)) {
        return;
    }

    const state = {
        sessions: loadSessions(),
        activeSessionId: localStorage.getItem(activeSessionKey)
    };
    ensureActiveSession(state);

    endpointInput.value = localStorage.getItem(endpointKey) || "https://api.deepseek.com";
    modelInput.value = localStorage.getItem(modelKey) || "deepseek-chat";
    apiKeyInput.value = sessionStorage.getItem(apiKeyStorageKey) || "";

    for (const input of [endpointInput, modelInput]) {
        input.addEventListener("change", () => saveProvider(endpointInput, modelInput, apiKeyInput));
    }
    apiKeyInput.addEventListener("change", () => saveProvider(endpointInput, modelInput, apiKeyInput));

    newSessionButton.addEventListener("click", () => {
        const session = createSession();
        state.sessions.unshift(session);
        state.sessions = pruneSessions(state.sessions);
        state.activeSessionId = session.id;
        saveState(state);
        render();
        messageInput.focus();
    });

    composer.addEventListener("submit", event => {
        event.preventDefault();
        void sendMessage({
            state,
            apiUrl,
            requestVerificationToken,
            endpointInput,
            modelInput,
            apiKeyInput,
            messageInput,
            chat,
            statusElement,
            submitButton,
            render
        });
    });

    messageInput.addEventListener("keydown", event => {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            composer.requestSubmit();
        }
    });

    render();

    function render() {
        renderSessionList(sessionList, state, () => {
            saveState(state);
            render();
        });
        renderChat(chat, getActiveSession(state));
    }
}

async function sendMessage({
    state,
    apiUrl,
    requestVerificationToken,
    endpointInput,
    modelInput,
    apiKeyInput,
    messageInput,
    chat,
    statusElement,
    submitButton,
    render
}) {
    const text = messageInput.value.trim();
    const provider = {
        endpoint: endpointInput.value.trim(),
        model: modelInput.value.trim(),
        apiKey: apiKeyInput.value.trim()
    };

    if (!text || !provider.endpoint || !provider.model || !provider.apiKey) {
        statusElement.textContent = "请填写模型配置和消息。";
        return;
    }

    saveProvider(endpointInput, modelInput, apiKeyInput);

    const session = getActiveSession(state);
    const userMessage = {
        role: "user",
        content: text
    };
    const pendingRound = {
        userText: text,
        assistantText: "",
        modelMessages: [userMessage],
        toolSteps: [],
        createdAt: new Date().toISOString()
    };

    messageInput.value = "";
    statusElement.textContent = "正在发送。";
    submitButton.disabled = true;
    renderPendingRound(chat, session, pendingRound);

    try {
        const requestMessages = flattenModelMessages(session).concat(userMessage);
        const response = await fetch(apiUrl, {
            method: "POST",
            headers: {
                Accept: "application/x-ndjson",
                "Content-Type": "application/json",
                RequestVerificationToken: requestVerificationToken ?? ""
            },
            credentials: "same-origin",
            body: JSON.stringify({
                provider,
                messages: requestMessages
            })
        });

        if (!response.ok || !response.body) {
            throw new Error(await readProblemMessage(response, "Agent 请求失败。"));
        }

        const result = await readAgentStream(response, pendingRound, statusElement, () => {
            renderPendingRound(chat, session, pendingRound);
        });

        if (result.errorMessage) {
            throw new Error(result.errorMessage);
        }
        if (!Array.isArray(result.modelMessages)) {
            throw new Error("Agent 响应缺少模型消息。");
        }

        pendingRound.modelMessages = [userMessage, ...result.modelMessages];
        session.rounds.push(pendingRound);
        session.rounds = session.rounds.slice(-maxRoundCount);
        session.title = session.title || createSessionTitle(text);
        session.updatedAt = new Date().toISOString();
        state.sessions = pruneSessions(moveSessionToTop(state.sessions, session.id));
        saveState(state);
        render();
        statusElement.textContent = "完成。";
    } catch (error) {
        statusElement.textContent = getErrorMessage(error);
        render();
    } finally {
        submitButton.disabled = false;
        messageInput.focus();
    }
}

async function readAgentStream(response, pendingRound, statusElement, onUpdate) {
    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = "";
    let modelMessages = null;
    let errorMessage = null;

    while (true) {
        const { done, value } = await reader.read();
        if (done) {
            break;
        }

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split("\n");
        buffer = lines.pop() ?? "";

        for (const line of lines) {
            if (!line.trim()) {
                continue;
            }

            const event = JSON.parse(line);
            const payload = event.payload;
            switch (event.type) {
                case "status":
                    statusElement.textContent = payload?.text ?? "";
                    break;
                case "tool_started":
                    pendingRound.toolSteps.push({
                        toolCallId: payload?.toolCallId,
                        name: payload?.name ?? "unknown",
                        summary: "正在查询",
                        status: "running",
                        elapsedMs: null
                    });
                    break;
                case "tool_finished":
                    updateToolStep(pendingRound.toolSteps, payload);
                    break;
                case "assistant_message":
                    pendingRound.assistantText = payload?.text ?? "";
                    break;
                case "model_messages":
                    modelMessages = payload?.messages;
                    break;
                case "error":
                    errorMessage = payload?.message ?? "Agent 请求失败。";
                    break;
            }

            onUpdate();
        }
    }

    return {
        modelMessages,
        errorMessage
    };
}

function updateToolStep(toolSteps, payload) {
    const toolCallId = payload?.toolCallId;
    const existingStep = toolSteps.find(step => step.toolCallId === toolCallId);
    if (!existingStep) {
        toolSteps.push({
            toolCallId,
            name: payload?.name ?? "unknown",
            summary: payload?.summary ?? "",
            status: payload?.status ?? "success",
            elapsedMs: payload?.elapsedMs ?? null
        });
        return;
    }

    existingStep.summary = payload?.summary ?? "";
    existingStep.status = payload?.status ?? "success";
    existingStep.elapsedMs = payload?.elapsedMs ?? null;
}

function renderSessionList(target, state, onChanged) {
    target.replaceChildren();

    for (const session of state.sessions) {
        const selectButton = createElement("button", {
            className: `agent-session__select ${session.id === state.activeSessionId ? "agent-session__select--active" : ""}`,
            children: [
                createElement("span", {
                    className: "agent-session__title",
                    text: session.title || "新会话"
                }),
                createElement("span", {
                    className: "agent-session__meta",
                    text: `${session.rounds.length} / ${maxRoundCount} 轮`
                })
            ]
        });
        selectButton.type = "button";
        selectButton.addEventListener("click", () => {
            state.activeSessionId = session.id;
            onChanged();
        });

        const deleteButton = createElement("button", {
            className: "agent-session__delete",
            text: "删除"
        });
        deleteButton.type = "button";
        deleteButton.addEventListener("click", () => {
            deleteSession(state, session.id);
            onChanged();
        });

        target.append(createElement("div", {
            className: "agent-session",
            children: [selectButton, deleteButton]
        }));
    }
}

function renderChat(target, session) {
    target.replaceChildren();

    if (!session || session.rounds.length === 0) {
        target.append(createElement("div", {
            className: "empty-state",
            text: "暂无对话。"
        }));
        return;
    }

    for (const round of session.rounds) {
        appendRound(target, round);
    }
    target.scrollTop = target.scrollHeight;
}

function renderPendingRound(target, session, pendingRound) {
    target.replaceChildren();

    if (session) {
        for (const round of session.rounds) {
            appendRound(target, round);
        }
    }

    appendRound(target, pendingRound);
    target.scrollTop = target.scrollHeight;
}

function appendRound(target, round) {
    target.append(createMessage("user", "用户", round.userText));
    const assistantChildren = [
        createElement("div", {
            className: "agent-message__meta",
            text: "助手"
        })
    ];

    if (Array.isArray(round.toolSteps) && round.toolSteps.length > 0) {
        assistantChildren.push(createToolSummary(round.toolSteps));
    }

    assistantChildren.push(createElement("div", {
        className: "agent-message__text",
        text: round.assistantText || "正在分析..."
    }));

    target.append(createElement("article", {
        className: "agent-message agent-message--assistant",
        children: assistantChildren
    }));
}

function createMessage(kind, label, text) {
    return createElement("article", {
        className: `agent-message agent-message--${kind}`,
        children: [
            createElement("div", {
                className: "agent-message__meta",
                text: label
            }),
            createElement("div", {
                className: "agent-message__text",
                text
            })
        ]
    });
}

function createToolSummary(toolSteps) {
    return createElement("div", {
        className: "agent-tool-summary",
        children: toolSteps.map(step => createElement("div", {
            className: "agent-tool-summary__item",
            text: `${getToolDisplayName(step.name)}：${step.summary || step.status}`
        }))
    });
}

function getToolDisplayName(name) {
    return {
        get_system_time: "系统时间",
        get_system_overview: "系统概览",
        list_devices: "设备列表",
        get_device_details: "设备详情",
        get_device_logs: "设备日志",
        get_log_details: "日志详情"
    }[name] ?? name;
}

function loadSessions() {
    try {
        const sessions = JSON.parse(localStorage.getItem(sessionsKey) || "[]");
        return Array.isArray(sessions) ? pruneSessions(sessions) : [];
    } catch {
        return [];
    }
}

function ensureActiveSession(state) {
    if (!state.sessions.some(session => session.id === state.activeSessionId)) {
        state.activeSessionId = state.sessions[0]?.id ?? null;
    }
    if (!state.activeSessionId) {
        const session = createSession();
        state.sessions = [session];
        state.activeSessionId = session.id;
        saveState(state);
    }
}

function deleteSession(state, sessionId) {
    state.sessions = state.sessions.filter(session => session.id !== sessionId);
    if (state.activeSessionId === sessionId) {
        state.activeSessionId = state.sessions[0]?.id ?? null;
    }
    ensureActiveSession(state);
}

function getActiveSession(state) {
    return state.sessions.find(session => session.id === state.activeSessionId) ?? state.sessions[0];
}

function createSession() {
    const now = new Date().toISOString();
    return {
        id: crypto.randomUUID(),
        title: "",
        createdAt: now,
        updatedAt: now,
        rounds: []
    };
}

function pruneSessions(sessions) {
    return sessions
        .map(session => ({
            ...session,
            rounds: Array.isArray(session.rounds) ? session.rounds.slice(-maxRoundCount) : []
        }))
        .sort((left, right) => String(right.updatedAt).localeCompare(String(left.updatedAt)))
        .slice(0, maxSessionCount);
}

function moveSessionToTop(sessions, sessionId) {
    const index = sessions.findIndex(session => session.id === sessionId);
    if (index <= 0) {
        return sessions;
    }

    const nextSessions = sessions.slice();
    const [session] = nextSessions.splice(index, 1);
    nextSessions.unshift(session);
    return nextSessions;
}

function saveState(state) {
    localStorage.setItem(sessionsKey, JSON.stringify(pruneSessions(state.sessions)));
    localStorage.setItem(activeSessionKey, state.activeSessionId ?? "");
}

function saveProvider(endpointInput, modelInput, apiKeyInput) {
    localStorage.setItem(endpointKey, endpointInput.value.trim());
    localStorage.setItem(modelKey, modelInput.value.trim());
    sessionStorage.setItem(apiKeyStorageKey, apiKeyInput.value.trim());
}

function flattenModelMessages(session) {
    return session.rounds.flatMap(round => Array.isArray(round.modelMessages) ? round.modelMessages : []);
}

function createSessionTitle(text) {
    return text.length > 24 ? `${text.slice(0, 24)}...` : text;
}

async function readProblemMessage(response, fallbackMessage) {
    const contentType = response.headers.get("content-type") ?? "";
    if (!contentType.includes("application/problem+json") && !contentType.includes("application/json")) {
        return fallbackMessage;
    }

    try {
        const problem = await response.json();
        return problem.detail || problem.title || fallbackMessage;
    } catch {
        return fallbackMessage;
    }
}

function getErrorMessage(error) {
    return error instanceof Error ? error.message : "Agent 请求失败。";
}

function createElement(tagName, { className, text, children = [] } = {}) {
    const element = document.createElement(tagName);

    if (className) {
        element.className = className;
    }
    if (text !== undefined) {
        element.textContent = String(text);
    }
    for (const child of children) {
        if (child) {
            element.append(child);
        }
    }

    return element;
}