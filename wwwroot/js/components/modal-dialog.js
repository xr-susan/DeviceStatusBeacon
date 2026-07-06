let nextModalDialogId = 1;

// 创建可复用的模态浮窗；复杂内容由调用方用 DOM 节点传入，组件只负责结构和生命周期
export function openModalDialog({
    title = "详情",
    content,
    footer,
    className = "",
    initialFocus
} = {}) {
    const dialogId = nextModalDialogId++;
    const titleId = `modal-dialog-title-${dialogId}`;

    const dialog = document.createElement("dialog");
    dialog.className = ["modal-dialog", className].filter(Boolean).join(" ");
    dialog.setAttribute("aria-labelledby", titleId);

    const titleElement = createElement("h2", {
        id: titleId,
        className: "modal-dialog__title",
        text: title
    });
    const contentElement = createElement("div", {
        className: "modal-dialog__content"
    });
    const footerElement = createElement("div", {
        className: "modal-dialog__footer"
    });

    if (content) {
        contentElement.append(content);
    }

    if (footer) {
        footerElement.append(footer);
    }

    dialog.append(
        createElement("div", {
            className: "modal-dialog__body",
            children: [titleElement, contentElement]
        }),
        footerElement
    );

    document.body.append(dialog);

    const close = () => {
        if (dialog.open) {
            dialog.close();
        }
    };

    dialog.addEventListener("close", () => dialog.remove(), {
        once: true
    });

    dialog.showModal();
    (initialFocus instanceof HTMLElement ? initialFocus : dialog.querySelector("button, input, textarea, select"))?.focus();

    return {
        dialog,
        contentElement,
        footerElement,
        close,
        setContent(nextContent) {
            contentElement.replaceChildren(nextContent);
        }
    };
}

export function createElement(tagName, { id, className, text, children = [], attributes = {} } = {}) {
    const element = document.createElement(tagName);

    if (id) {
        element.id = id;
    }

    if (className) {
        element.className = className;
    }

    if (text !== undefined) {
        element.textContent = String(text);
    }

    for (const [name, value] of Object.entries(attributes)) {
        if (value !== undefined && value !== null) {
            element.setAttribute(name, String(value));
        }
    }

    for (const child of children) {
        if (child) {
            element.append(child);
        }
    }

    return element;
}