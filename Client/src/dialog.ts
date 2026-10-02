import { Counterparty, CounterpartyService, CounterpartyViewState, CounterpartyWidget } from "./counterparty";

export function openCounterpartyDialog(service: CounterpartyService, cardId: string): HTMLDialogElement {
    const dialog = document.createElement("dialog");
    dialog.setAttribute("aria-label", "Данные контрагента CRM");
    const title = document.createElement("h2");
    title.textContent = "Контрагент CRM";
    const form = document.createElement("form");
    const label = document.createElement("label");
    label.textContent = "ID контрагента ";
    const input = document.createElement("input");
    input.required = true;
    input.name = "counterpartyId";
    input.autocomplete = "off";
    label.append(input);
    const submit = document.createElement("button");
    submit.type = "submit";
    submit.textContent = "Получить и сохранить";
    const close = document.createElement("button");
    close.type = "button";
    close.textContent = "Закрыть";
    const status = document.createElement("p");
    status.setAttribute("role", "status");
    status.setAttribute("aria-live", "polite");
    const details = document.createElement("dl");
    form.append(label, submit);
    dialog.append(title, form, status, details, close);

    let loading = false;
    const render = (state: CounterpartyViewState): void => {
        if (!dialog.isConnected) return;
        loading = state.loading;
        input.disabled = submit.disabled = loading;
        dialog.setAttribute("aria-busy", String(loading));
        status.textContent = loading ? "Загрузка…" : state.error || (state.counterparty ? "Данные сохранены в карточке." : "");
        details.replaceChildren();
        if (!state.counterparty) return;
        const fields: [keyof Counterparty, string][] = [
            ["id", "ID"], ["name", "Наименование"], ["inn", "ИНН"], ["kpp", "КПП"],
            ["legalAddress", "Юридический адрес"], ["phone", "Телефон"], ["email", "Email"]
        ];
        for (const [key, caption] of fields) {
            const term = document.createElement("dt");
            term.textContent = caption;
            const value = document.createElement("dd");
            // CRM values are text, never HTML.
            value.textContent = state.counterparty[key] || "—";
            details.append(term, value);
        }
    };
    form.addEventListener("submit", event => {
        event.preventDefault();
        if (loading) return;
        const widget = new CounterpartyWidget(service, cardId, input.value, render);
        void widget.load();
    });
    close.addEventListener("click", () => dialog.close());
    dialog.addEventListener("close", () => dialog.remove(), { once: true });
    document.body.append(dialog);
    dialog.showModal();
    input.focus();
    return dialog;
}
