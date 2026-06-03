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