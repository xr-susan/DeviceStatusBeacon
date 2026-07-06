import { showConfirmDialog } from "../components/confirm-dialog.js";
import { createElement, openModalDialog } from "../components/modal-dialog.js";

const deviceNamePlaceholder = "DeviceNamePlaceholder";
const userNamePlaceholder = "UserNamePlaceholder";
const identityNamePattern = /^[A-Za-z\d][A-Za-z\d_\-]{2,62}[A-Za-z\d]$/;
const identityNameTitle = "设备名称长度为 4 到 64 个字符，只能包含字母、数字、下划线和连字符，且首尾必须是字母或数字。";
const root = document.querySelector("[data-log-details-root]");

if (root instanceof HTMLElement) {
    const apiBase = root.dataset.logApiBase;
    const deviceDetailsUrlTemplate = root.dataset.deviceDetailsUrlTemplate;
    const userDetailsUrlTemplate = root.dataset.userDetailsUrlTemplate;
    const requestVerificationToken = root.dataset.requestVerificationToken;
    const canManageLogs = root.dataset.canManageLogs === "true";
    const canViewUsers = root.dataset.canViewUsers === "true";
    const simulatedLogButton = root.querySelector("[data-simulated-log-button]");

    if (canManageLogs && simulatedLogButton && apiBase) {
        simulatedLogButton.addEventListener("click", () => {
            openSimulatedLogDialog(apiBase, requestVerificationToken);
        });
    }

    if (apiBase) {
        for (const button of root.querySelectorAll("[data-log-id]")) {
            button.addEventListener("click", () => {
                const logId = button.getAttribute("data-log-id");
                if (logId) {
                    void openLogDetails(apiBase, logId, {
                        deviceDetailsUrlTemplate,
                        userDetailsUrlTemplate,
                        requestVerificationToken,
                        canManageLogs,
                        canViewUsers,
                        sourceButton: button
                    });
                }
            });
        }
    }
}

function openSimulatedLogDialog(apiUrl, requestVerificationToken) {
    const deviceNameInput = createElement("input", {
        className: "field__input",
        attributes: {
            id: "simulated-log-device-name",
            minlength: 4,
            maxlength: 64,
            pattern: "[A-Za-z\\d][A-Za-z\\d_\\-]{2,62}[A-Za-z\\d]",
            placeholder: "设备名称",
            title: identityNameTitle
        }
    });

    const addressesInput = createElement("textarea", {
        className: "field__input simulated-log-editor__input",
        attributes: {
            id: "simulated-log-addresses",
            rows: 4,
            placeholder: "192.168.1.10, 10.0.0.5"
        }
    });

    const messageInput = createElement("textarea", {
        className: "field__input simulated-log-editor__input",
        attributes: {
            id: "simulated-log-message",
            rows: 4,
            maxlength: 256,
            placeholder: "可选"
        }
    });

    const errorElement = createElement("div", {
        className: "simulated-log-editor__error"
    });
    const cancelButton = createButton("取消", "button button--secondary");
    const submitButton = createButton("提交", "button button--primary");

    const modal = openModalDialog({
        title: "模拟提交日志",
        content: createElement("div", {
            className: "simulated-log-editor",
            children: [
                createField("设备名称", deviceNameInput),
                createField("IP 地址列表", addressesInput),
                createField("提交消息", messageInput),
                errorElement
            ]
        }),
        footer: createElement("div", {
            className: "modal-dialog__button-row",
            children: [cancelButton, submitButton]
        }),
        className: "simulated-log-editor-dialog",
        initialFocus: deviceNameInput
    });

    cancelButton.addEventListener("click", () => modal.close());
    submitButton.addEventListener("click", () => {
        void submitSimulatedLog(apiUrl, requestVerificationToken, {
            deviceNameInput,
            addressesInput,
            messageInput,
            errorElement,
            cancelButton,
            submitButton
        });
    });
}

async function submitSimulatedLog(apiUrl, requestVerificationToken, {
    deviceNameInput,
    addressesInput,
    messageInput,
    errorElement,
    cancelButton,
    submitButton
}) {
    errorElement.textContent = "";

    const deviceName = deviceNameInput.value.trim();
    const addressList = parseAddressList(addressesInput.value);
    const reportedAddresses = addressList.addresses;
    const message = messageInput.value.trim() || null;

    if (!deviceName) {
        errorElement.textContent = "请填写设备名称。";
        return;
    }

    if (!identityNamePattern.test(deviceName)) {
        errorElement.textContent = identityNameTitle;
        return;
    }

    if (reportedAddresses.length === 0) {
        errorElement.textContent = "请至少填写 1 个 IP 地址。";
        return;
    }

    if (addressList.hasEmptyAddress) {
        errorElement.textContent = "IP 地址不能为空。";
        return;
    }

    setButtonsDisabled([cancelButton, submitButton], true);
    try {
        const response = await fetch(`${apiUrl}/by-name/${encodeURIComponent(deviceName)}`, {
            method: "POST",
            headers: {
                Accept: "application/json",
                "Content-Type": "application/json",
                RequestVerificationToken: requestVerificationToken ?? ""
            },
            credentials: "same-origin",
            body: JSON.stringify({
                reportedAddresses,
                message
            })
        });

        if (!response.ok) {
            throw new Error(await readProblemMessage(response, "日志提交失败。"));
        }

        window.location.reload();
    } catch (error) {
        errorElement.textContent = getErrorMessage(error);
        setButtonsDisabled([cancelButton, submitButton], false);
    }
}

function createField(label, input) {
    const labelElement = createElement("label", {
        className: "field__label",
        text: label,
        attributes: {
            for: input.id
        }
    });

    return createElement("div", {
        className: "field",
        children: [labelElement, input]
    });
}

function parseAddressList(value) {
    const normalizedValue = value.trim();
    if (!normalizedValue) {
        return {
            addresses: [],
            hasEmptyAddress: false
        };
    }

    const addresses = normalizedValue
        .split(/[,，\n]/)
        .map(address => address.trim());

    return {
        addresses,
        hasEmptyAddress: addresses.some(address => !address)
    };
}

async function openLogDetails(apiBase, logId, options) {
    const { sourceButton } = options;
    const modal = openLoadingLogDetailsDialog(logId);

    sourceButton.disabled = true;
    try {
        const onlineLog = await fetchOnlineLog(apiBase, logId);
        updateLogDetailsDialog(apiBase, modal, onlineLog, options);
    } catch (error) {
        showLogDetailsError(modal, getErrorMessage(error));
    } finally {
        sourceButton.disabled = false;
    }
}

async function fetchOnlineLog(apiBase, logId) {
    const response = await fetch(`${apiBase}/${encodeURIComponent(logId)}`, {
        headers: {
            Accept: "application/json"
        },
        credentials: "same-origin"
    });

    if (!response.ok) {
        throw new Error(await readProblemMessage(response, "未能读取日志详情。"));
    }

    return await response.json();
}

function openLoadingLogDetailsDialog(logId) {
    const closeButton = createButton("关闭", "button button--secondary");
    let modal;

    closeButton.addEventListener("click", () => modal.close());

    modal = openModalDialog({
        title: `日志 ${logId}`,
        content: createElement("div", {
            className: "empty-state empty-state--left",
            text: "正在加载日志详情..."
        }),
        footer: createElement("div", {
            className: "modal-dialog__button-row",
            children: [closeButton]
        }),
        className: "modal-dialog--wide log-details-dialog"
    });

    return modal;
}

function updateLogDetailsDialog(apiBase, modal, onlineLog, options) {
    const {
        deviceDetailsUrlTemplate,
        userDetailsUrlTemplate,
        requestVerificationToken,
        canManageLogs,
        canViewUsers,
        sourceButton
    } = options;

    const closeButton = createButton("关闭", "button button--secondary");
    const footerChildren = [closeButton];

    if (canManageLogs) {
        const editButton = createButton("修改消息", "button button--secondary");
        const deleteButton = createButton("删除日志", "button button--danger");

        editButton.addEventListener("click", () => {
            openEditMessageDialog(apiBase, onlineLog, requestVerificationToken, updatedMessage => {
                onlineLog.message = updatedMessage;
                modal.setContent(createLogDetailsContent(onlineLog, {
                    canViewUsers,
                    deviceDetailsUrlTemplate,
                    userDetailsUrlTemplate
                }));
                updateLogListMessage(sourceButton, updatedMessage);
            });
        });

        deleteButton.addEventListener("click", () => {
            void confirmAndDeleteLog(apiBase, onlineLog, requestVerificationToken, modal);
        });

        footerChildren.unshift(deleteButton, editButton);
    }

    closeButton.addEventListener("click", () => modal.close());

    modal.setContent(createLogDetailsContent(onlineLog, {
        canViewUsers,
        deviceDetailsUrlTemplate,
        userDetailsUrlTemplate
    }));
    modal.footerElement.replaceChildren(createElement("div", {
        className: "modal-dialog__button-row",
        children: footerChildren
    }));
}

function showLogDetailsError(modal, message) {
    modal.setContent(createElement("div", {
        className: "notice notice--danger",
        text: message
    }));
}

function createLogDetailsContent(onlineLog, { canViewUsers, deviceDetailsUrlTemplate, userDetailsUrlTemplate }) {
    const reportedAddresses = Array.isArray(onlineLog.reportedAddresses)
        ? onlineLog.reportedAddresses
        : [];
    const deviceUrl = createTemplateUrl(deviceDetailsUrlTemplate, deviceNamePlaceholder, onlineLog.deviceName);
    const userUrl = canViewUsers
        ? createTemplateUrl(userDetailsUrlTemplate, userNamePlaceholder, onlineLog.submittedByUserName)
        : null;

    return createElement("div", {
        className: "log-details",
        children: [
            createDetailsGrid([
                ["日志 ID", onlineLog.onlineLogId, "code-text"],
                ["设备名称", createEntityIdentity({
                    displayName: onlineLog.deviceDisplayName,
                    name: onlineLog.deviceName,
                    href: deviceUrl
                })],
                ["日志时间", formatLocalDateTime(onlineLog.logTime)],
                ["来源地址", onlineLog.reporterRemoteAddress || "未知", "code-text"],
                ["提交者", createSubmittedByElement(onlineLog, userUrl)],
                ["提交消息", getMessageText(onlineLog.message)]
            ]),
            createElement("section", {
                className: "log-details__section",
                children: [
                    createElement("h3", {
                        className: "log-details__section-title",
                        text: "完整 IP 地址列表"
                    }),
                    createAddressList(reportedAddresses)
                ]
            })
        ]
    });
}

function createDetailsGrid(rows) {
    const list = createElement("dl", {
        className: "log-details__grid"
    });

    for (const [label, value, valueClassName] of rows) {
        const row = createElement("div", {
            className: "log-details__row",
            children: [
                createElement("dt", {
                    text: label
                })
            ]
        });

        const valueElement = createElement("dd", {
            className: valueClassName
        });

        if (value instanceof Node) {
            valueElement.append(value);
        } else {
            valueElement.textContent = value ?? "未知";
        }

        row.append(valueElement);
        list.append(row);
    }

    return list;
}

function createAddressList(addresses) {
    if (addresses.length === 0) {
        return createElement("div", {
            className: "empty-state empty-state--left",
            text: "暂无地址。"
        });
    }

    return createElement("ul", {
        className: "log-details__address-list",
        children: addresses.map(address => createElement("li", {
            className: "code-text",
            text: address
        }))
    });
}

function createSubmittedByElement(onlineLog, userUrl) {
    const userName = onlineLog.submittedByUserName;
    const displayName = onlineLog.submittedByUserDisplayName;

    if (!userName && !displayName) {
        return createElement("span", {
            className: "log-details__muted",
            text: "提交者为设备自身或提交用户已不存在"
        });
    }

    return createEntityIdentity({
        displayName,
        name: userName ?? "用户名未知",
        href: userName ? userUrl : null
    });
}

function createEntityIdentity({ displayName, name, href }) {
    const children = [];

    if (displayName) {
        children.push(createElement("span", {
            className: "entity-identity__display",
            text: displayName
        }));
    }

    if (href) {
        children.push(createElement("a", {
            className: "entity-identity__name entity-identity__link",
            text: name,
            attributes: {
                href
            }
        }));
    } else {
        children.push(createElement("span", {
            className: "entity-identity__name",
            text: name
        }));
    }

    return createElement("div", {
        className: "entity-identity",
        children
    });
}

function openEditMessageDialog(apiBase, onlineLog, requestVerificationToken, onUpdated) {
    const originalMessage = onlineLog.message ?? "";
    const textarea = createElement("textarea", {
        className: "field__input log-message-editor__input",
        attributes: {
            rows: 5,
            maxlength: 256,
            placeholder: originalMessage || "留空将删除消息"
        }
    });
    textarea.value = originalMessage;

    const errorElement = createElement("div", {
        className: "log-message-editor__error"
    });
    const cancelButton = createButton("取消", "button button--secondary");
    const saveButton = createButton("保存", "button button--primary");

    const modal = openModalDialog({
        title: `修改日志 ${onlineLog.onlineLogId} 的消息`,
        content: createElement("div", {
            className: "log-message-editor",
            children: [
                createElement("label", {
                    className: "field__label",
                    text: "提交消息",
                    attributes: {
                        for: "log-message-editor-input"
                    }
                }),
                textarea,
                createElement("p", {
                    className: "log-message-editor__hint",
                    text: "留空保存时将删除该日志的消息。"
                }),
                errorElement
            ]
        }),
        footer: createElement("div", {
            className: "modal-dialog__button-row",
            children: [cancelButton, saveButton]
        }),
        className: "log-message-editor-dialog",
        initialFocus: textarea
    });

    textarea.id = "log-message-editor-input";
    cancelButton.addEventListener("click", () => modal.close());
    saveButton.addEventListener("click", () => {
        void saveMessage(apiBase, onlineLog.onlineLogId, textarea.value, requestVerificationToken, {
            saveButton,
            cancelButton,
            errorElement,
            modal,
            onUpdated
        });
    });
}

async function saveMessage(apiBase, onlineLogId, value, requestVerificationToken, { saveButton, cancelButton, errorElement, modal, onUpdated }) {
    const nextMessage = value.trim() || null;
    setButtonsDisabled([saveButton, cancelButton], true);
    errorElement.textContent = "";

    try {
        const response = await fetch(`${apiBase}/${encodeURIComponent(onlineLogId)}/message`, {
            method: "PUT",
            headers: {
                Accept: "application/json",
                "Content-Type": "application/json",
                RequestVerificationToken: requestVerificationToken ?? ""
            },
            credentials: "same-origin",
            body: JSON.stringify({
                message: nextMessage
            })
        });

        if (!response.ok) {
            throw new Error(await readProblemMessage(response, "消息更新失败。"));
        }

        onUpdated(nextMessage);
        modal.close();
    } catch (error) {
        errorElement.textContent = getErrorMessage(error);
        setButtonsDisabled([saveButton, cancelButton], false);
    }
}

async function confirmAndDeleteLog(apiBase, onlineLog, requestVerificationToken, detailsModal) {
    const result = await showConfirmDialog({
        title: "删除日志",
        message: `此操作会删除日志 ${onlineLog.onlineLogId}，并可能刷新设备的最新状态摘要。`,
        confirmButtonText: "删除",
        variant: "danger",
        confirmOnInputEnter: false
    });

    if (!result.confirmed) {
        return;
    }

    try {
        const response = await fetch(`${apiBase}/${encodeURIComponent(onlineLog.onlineLogId)}`, {
            method: "DELETE",
            headers: {
                Accept: "application/json",
                RequestVerificationToken: requestVerificationToken ?? ""
            },
            credentials: "same-origin"
        });

        if (!response.ok) {
            throw new Error(await readProblemMessage(response, "日志删除失败。"));
        }

        removeLogListItem(onlineLog.onlineLogId);
        detailsModal.close();
    } catch (error) {
        await showConfirmDialog({
            title: "日志删除失败",
            message: getErrorMessage(error),
            confirmButtonText: "知道了",
            hideCancel: true
        });
    }
}

function updateLogListMessage(sourceButton, message) {
    const item = sourceButton.closest("[data-log-item]");
    const target = item?.querySelector("[data-log-message-value]");
    if (target) {
        target.textContent = getMessageText(message);
    }
}

function removeLogListItem(onlineLogId) {
    const item = document.querySelector(`[data-log-item="${CSS.escape(String(onlineLogId))}"]`);
    item?.remove();
}

function createButton(text, className) {
    const button = createElement("button", {
        className,
        text
    });
    button.type = "button";
    return button;
}

function setButtonsDisabled(buttons, disabled) {
    for (const button of buttons) {
        button.disabled = disabled;
    }
}

function getMessageText(message) {
    return message?.trim() || "暂无消息";
}

function createTemplateUrl(template, placeholder, value) {
    if (!template || !value) {
        return null;
    }

    return template.replace(placeholder, encodeURIComponent(value));
}

function formatLocalDateTime(value) {
    if (!value) {
        return "未知";
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return "未知";
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
    return error instanceof Error ? error.message : "操作失败，请稍后重试。";
}