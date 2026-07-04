let nextDialogId = 1;

// 创建一次性的确认弹窗实例；调用方可以按需传入标题、正文、确认按钮和输入校验
export function createConfirmDialog(defaultOptions = {}) {
    return {
        open(options = {}) {
            return openConfirmDialog({
                ...defaultOptions,
                ...options
            });
        }
    };
}

export function showConfirmDialog(options = {}) {
    return createConfirmDialog().open(options);
}

function openConfirmDialog({
    title = "确认操作",
    message = "",
    confirmButtonText = "确认",
    cancelButtonText = "取消",
    variant = "default",
    hideCancel = false,
    confirmOnInputEnter,
    input
} = {}) {
    const dialogId = nextDialogId++;
    const titleId = `confirm-dialog-title-${dialogId}`;
    const messageId = `confirm-dialog-message-${dialogId}`;

    const dialog = document.createElement("dialog");
    dialog.className = "confirm-dialog";
    dialog.setAttribute("aria-labelledby", titleId);
    dialog.setAttribute("aria-describedby", messageId);

    const titleElement = createElement("h2", {
        id: titleId,
        className: "confirm-dialog__title",
        text: title
    });
    const messageElement = createElement("p", {
        id: messageId,
        className: "confirm-dialog__message",
        text: message
    });

    const bodyChildren = [titleElement, messageElement];
    let inputElement = null;
    let errorElement = null;
    if (input) {
        const inputId = `confirm-dialog-input-${dialogId}`;
        inputElement = createElement("input", {
            id: inputId,
            className: "field__input confirm-dialog__input"
        });
        inputElement.type = "text";
        inputElement.autocomplete = "off";
        inputElement.placeholder = input.placeholder ?? "";

        errorElement = createElement("div", {
            className: "confirm-dialog__error",
            text: ""
        });

        bodyChildren.push(
            createElement("label", {
                className: "field__label confirm-dialog__label",
                text: input.label ?? "确认输入",
                htmlFor: inputId
            }),
            inputElement,
            errorElement
        );
    }

    const cancelButton = createElement("button", {
        className: "button button--secondary",
        text: cancelButtonText
    });
    cancelButton.type = "button";

    const confirmButton = createElement("button", {
        className: variant === "danger" ? "button button--danger" : "button button--primary",
        text: confirmButtonText
    });
    confirmButton.type = "button";

    const footerChildren = hideCancel ? [confirmButton] : [cancelButton, confirmButton];
    dialog.append(
        createElement("div", {
            className: "confirm-dialog__body",
            children: bodyChildren
        }),
        createElement("div", {
            className: "confirm-dialog__footer",
            children: footerChildren
        })
    );

    document.body.append(dialog);

    return new Promise(resolve => {
        let resolved = false;
        const shouldConfirmOnInputEnter = confirmOnInputEnter ?? (variant !== "danger");

        const disableButtons = () => {
            cancelButton.disabled = true;
            confirmButton.disabled = true;
        };

        const finish = result => {
            if (resolved) {
                return;
            }

            resolved = true;
            disableButtons();
            resolve(result);
            dialog.close();
        };

        cancelButton.addEventListener("click", () => finish({
            confirmed: false,
            inputValue: null
        }));

        const confirm = () => {
            if (resolved) {
                return;
            }

            const inputValue = inputElement?.value.trim() ?? null;
            if (input?.expectedValue !== undefined && inputValue !== input.expectedValue) {
                if (errorElement) {
                    errorElement.textContent = input.mismatchMessage ?? "输入内容不匹配。";
                }
                inputElement?.focus();
                inputElement?.select();
                return;
            }

            finish({
                confirmed: true,
                inputValue
            });
        };

        confirmButton.addEventListener("click", confirm);

        inputElement?.addEventListener("keydown", event => {
            if (!shouldConfirmOnInputEnter || event.key !== "Enter" || event.isComposing) {
                return;
            }

            event.preventDefault();
            confirm();
        });

        dialog.addEventListener("cancel", event => {
            event.preventDefault();
            finish({
                confirmed: false,
                inputValue: null
            });
        });

        dialog.addEventListener("close", () => dialog.remove(), {
            once: true
        });

        dialog.showModal();
        (inputElement ?? confirmButton).focus();
    });
}

function createElement(tagName, { id, className, text, children = [], htmlFor } = {}) {
    const element = document.createElement(tagName);

    if (id) {
        element.id = id;
    }

    if (className) {
        element.className = className;
    }

    if (htmlFor !== undefined && element instanceof HTMLLabelElement) {
        element.htmlFor = htmlFor;
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