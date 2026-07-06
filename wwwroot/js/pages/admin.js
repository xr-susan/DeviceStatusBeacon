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

    const result = await showConfirmDialog({
        title: form.dataset.confirmTitle,
        message: form.dataset.confirmMessage,
        confirmButtonText: form.dataset.confirmButtonText,
        variant: form.dataset.confirmVariant
    });

    if (!result.confirmed) {
        return;
    }

    if (form.matches("[data-delete-user-form]")) {
        const userName = form.dataset.userName ?? "";
        const deleteConfirmation = await showConfirmDialog({
            title: "输入用户名确认删除",
            message: "请输入完整用户名以继续删除。",
            confirmButtonText: "删除用户",
            variant: "danger",
            input: {
                label: "用户名",
                placeholder: userName,
                expectedValue: userName,
                mismatchMessage: "输入的用户名与当前用户不一致。"
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