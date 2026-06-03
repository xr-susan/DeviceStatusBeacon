const activityTarget = document.getElementById("dashboard-activity");
const totalLogCountTarget = document.getElementById("dashboard-total-log-count");

// Dashboard 页面是单实例结构，直接按约定的元素 ID 读取即可
if (activityTarget instanceof HTMLElement) {
    const activityUrl = activityTarget.dataset.activityUrl;
    if (activityUrl) {
        void loadDashboardActivity(activityUrl, activityTarget, totalLogCountTarget);
    }
}

// 加载 Dashboard 最近活动数据，并在成功后刷新对应内容区域
async function loadDashboardActivity(activityUrl, target, totalLogCountTarget) {
    try {
        const response = await fetch(activityUrl, {
            headers: {
                Accept: "application/json"
            },
            credentials: "same-origin"
        });

        if (!response.ok) {
            throw new Error(`Unexpected response: ${response.status}`);
        }

        const data = await response.json();
        renderDashboardActivity(target, data, totalLogCountTarget);
    } catch {
        // 按需摘要与最近活动共享同一请求，失败时统一回落到占位值
        setMetricValue(totalLogCountTarget, "-");
        target.dataset.activityState = "error";
        target.replaceChildren(createEmptyState("最近活动加载失败，请稍后刷新页面重试。"));
    }
}

// 把最近活动响应数据渲染为设备与日志两列内容
function renderDashboardActivity(target, data, totalLogCountTarget) {
    // 对外部 JSON 做最小结构约束，避免异常响应直接破坏页面渲染
    const accessibleLogCount = Number.isInteger(data.accessibleLogCount) ? data.accessibleLogCount : "-";
    const recentDevices = Array.isArray(data.recentDevices) ? data.recentDevices : [];
    const recentLogs = Array.isArray(data.recentLogs) ? data.recentLogs : [];
    setMetricValue(totalLogCountTarget, accessibleLogCount);

    // 最近活动区域的顶层结构固定为两列面板，分别承载设备与日志数据
    const grid = createElement("div", {
        className: "dashboard-activity__grid",
        children: [
            createActivityPanel("设备", renderRecentDevices(recentDevices)),
            createActivityPanel("日志", renderRecentLogs(recentLogs))
        ]
    });

    target.dataset.activityState = "ready";
    target.replaceChildren(grid);
}

// 把延后加载的摘要指标同步到顶部概览卡片
function setMetricValue(target, value) {
    if (target instanceof HTMLElement) {
        target.textContent = String(value);
    }
}

// 渲染最近活跃设备列表
function renderRecentDevices(devices) {
    if (devices.length === 0) {
        return createEmptyState("暂无设备活动。");
    }

    // 设备列表本身只是顺序容器，单项结构由 article 节点承载
    const list = createElement("div", { className: "dashboard-device-list" });
    for (const device of devices) {
        const deviceName = device.deviceName ?? "";
        const displayName = device.displayName?.trim() || "未设置显示名称";
        const latestLogTime = formatLocalDateTime(device.latestLogTime);
        const enabledText = device.enabled ? "启用" : "停用";
        const enabledClass = device.enabled ? "status-pill--success" : "status-pill--muted";

        // 使用 textContent 路径组装纯文本字段，避免依赖模板拼接和 HTML 转义
        const article = createElement("article", {
            className: "dashboard-device-item",
            children: [
                createElement("strong", { text: deviceName }),
                createElement("div", {
                    className: "table-subtext",
                    text: displayName
                }),
                createElement("div", {
                    className: "dashboard-device-item__meta",
                    children: [
                        createElement("span", {
                            className: `status-pill ${enabledClass}`,
                            text: enabledText
                        }),
                        createElement("span", { text: latestLogTime })
                    ]
                })
            ]
        });

        list.append(article);
    }

    return list;
}

// 渲染最近日志列表
function renderRecentLogs(logs) {
    if (logs.length === 0) {
        return createEmptyState("暂无日志活动。");
    }

    // 日志列表沿用站点现有 activity-item 结构，保持与日志页视觉语义一致
    const list = createElement("div", { className: "activity-list" });
    for (const log of logs) {
        const deviceName = log.deviceName ?? "";
        const deviceDisplayName = log.deviceDisplayName?.trim() || "未设置显示名称";
        const logTime = formatLocalDateTime(log.logTime);
        const onlineLogId = String(log.onlineLogId ?? "");
        const addresses = Array.isArray(log.reportedAddresses) && log.reportedAddresses.length > 0
            ? log.reportedAddresses.join(", ")
            : "暂无地址";

        // 详情区按“地址 / 来源 / 消息”逐项追加，避免条件块和字符串拼接交织
        const details = createElement("div", { className: "activity-item__details" });
        details.append(createLabeledDetail("地址", addresses));

        if (log.reporterRemoteAddress) {
            details.append(createLabeledDetail("来源", log.reporterRemoteAddress));
        }

        if (log.message) {
            details.append(createLabeledDetail("消息", log.message));
        }

        // 单条日志由头部摘要和详情区组成，两部分都通过固定节点骨架拼装
        const article = createElement("article", {
            className: "activity-item",
            children: [
                createElement("div", {
                    className: "activity-item__row",
                    children: [
                        createElement("div", {
                            children: [
                                createElement("strong", { text: deviceName }),
                                createElement("div", {
                                    className: "table-subtext",
                                    text: deviceDisplayName
                                })
                            ]
                        }),
                        createElement("div", {
                            className: "activity-item__meta",
                            children: [
                                createElement("span", { text: logTime }),
                                createElement("span", {
                                    className: "code-text",
                                    text: `#${onlineLogId}`
                                })
                            ]
                        })
                    ]
                }),
                details
            ]
        });

        list.append(article);
    }

    return list;
}

// 将服务端返回的时间值格式化为本地可读时间文本
function formatLocalDateTime(value) {
    if (!value) {
        return "暂无日志";
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return "时间未知";
    }

    return new Intl.DateTimeFormat("zh-CN", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
        hour12: false
    }).format(date);
}

// 创建最近活动两列布局中的单个面板
function createActivityPanel(title, content) {
    return createElement("div", {
        className: "dashboard-activity__panel",
        children: [
            createElement("strong", {
                className: "dashboard-activity__title",
                text: title
            }),
            content
        ]
    });
}

// 创建用于“标签 + 值”结构的详情行
function createLabeledDetail(label, value) {
    // 标签仍使用独立 span，方便复用现有 detail-label 样式
    const row = document.createElement("div");
    row.append(
        createElement("span", {
            className: "detail-label",
            text: label
        }),
        String(value)
    );

    return row;
}

// 创建统一的空状态占位块
function createEmptyState(message) {
    return createElement("div", {
        className: "empty-state",
        text: message
    });
}

// 创建带可选类名、文本和子节点的 HTML 元素
function createElement(tagName, { className, text, children = [] } = {}) {
    const element = document.createElement(tagName);

    if (className) {
        element.className = className;
    }

    if (text !== undefined) {
        // 统一走 textContent，把动态值限制为纯文本写入
        element.textContent = String(text);
    }

    for (const child of children) {
        if (child) {
            // 只接受已构造完成的节点，避免把原始字符串混入 DOM 构造路径
            element.append(child);
        }
    }

    return element;
}