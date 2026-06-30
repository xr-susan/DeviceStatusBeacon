// 标记当前页面已启用 JavaScript，供样式或行为分支按需识别
document.documentElement.dataset.js = "enabled";

// 为声明了自动全选语义的输入框挂接一次性 focus 行为
for (const input of document.querySelectorAll("[data-select-on-focus]")) {
    input.addEventListener("focus", () => {
        // 仅对文本输入框执行 select，避免把其他元素误当作可选中文本控件
        if (input instanceof HTMLInputElement) {
            input.select();
        }
    }, { once: true });
}

// 将服务端输出的 UTC 时间按浏览器时区格式化；无法解析时保留原始 UTC 文本
const localDateTimeFormatter = new Intl.DateTimeFormat("zh-CN", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false
});

for (const timeElement of document.querySelectorAll("time[data-local-date-time]")) {
    const dateTime = timeElement.getAttribute("datetime");
    if (!dateTime) {
        continue;
    }

    const date = new Date(dateTime);
    if (Number.isNaN(date.getTime())) {
        continue;
    }

    timeElement.textContent = localDateTimeFormatter.format(date);
}