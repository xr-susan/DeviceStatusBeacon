import { showConfirmDialog } from "../components/confirm-dialog.js";

for (const form of document.querySelectorAll("form[data-confirm-form]")) {
    form.addEventListener("submit", event => {
        if (form.dataset.submitInProgress === "true") {
            return;
        }

        if (form.dataset.confirmInProgress === "true") {
            event.preventDefault();
            return;
        }

        if (!form.checkValidity()) {
            event.preventDefault();
            form.reportValidity();
            return;
        }

        event.preventDefault();
        form.dataset.confirmInProgress = "true";
        void confirmAndSubmit(form).finally(() => {
            if (form.dataset.submitInProgress !== "true") {
                delete form.dataset.confirmInProgress;
            }
        });
    });
}

async function confirmAndSubmit(form) {
    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    if (form.matches("[data-delete-device-form]") && form.dataset.canBeDelete !== "true") {
        await showConfirmDialog({
            title: "暂不能删除设备",
            message: form.dataset.deleteBlockingMessage || "当前设备不符合删除条件。",
            confirmButtonText: "知道了",
            hideCancel: true
        });
        return;
    }

    const firstConfirmation = await showConfirmDialog({
        title: form.dataset.confirmTitle,
        message: form.dataset.confirmMessage,
        confirmButtonText: form.dataset.confirmButtonText,
        variant: form.dataset.confirmVariant
    });
    if (!firstConfirmation.confirmed) {
        return;
    }

    if (form.matches("[data-delete-device-form]")) {
        const deviceName = form.dataset.deviceName ?? "";
        const deleteConfirmation = await showConfirmDialog({
            title: "输入设备名称确认删除",
            message: "请输入完整设备名称以继续删除。",
            confirmButtonText: "删除设备",
            variant: "danger",
            input: {
                label: "设备名称",
                placeholder: deviceName,
                expectedValue: deviceName,
                mismatchMessage: "输入的设备名称与当前设备不一致。"
            }
        });

        if (!deleteConfirmation.confirmed) {
            return;
        }

        const confirmationInput = form.querySelector("[data-delete-confirmation-input]");
        if (confirmationInput instanceof HTMLInputElement) {
            confirmationInput.value = deleteConfirmation.inputValue ?? "";
        }
    }

    form.dataset.submitInProgress = "true";
    disableFormButtons(form);
    form.requestSubmit();
}

function disableFormButtons(form) {
    for (const button of form.querySelectorAll("button")) {
        button.disabled = true;
        button.setAttribute("aria-disabled", "true");
    }
}