import { CounterpartyResponse, CounterpartyService } from "./counterparty";
import { openCounterpartyDialog } from "./dialog";

const scenario = document.querySelector<HTMLSelectElement>("#scenario")!;
const delay = document.querySelector<HTMLSelectElement>("#delay")!;
const open = document.querySelector<HTMLButtonElement>("#open-dialog")!;

// Replace only the WebClient transport. The service, validation and dialog are shared with the extension.
const service = new CounterpartyService({
    post: async <T>(_url: string, body: string): Promise<T> => {
        const selectedScenario = scenario.value;
        const { counterpartyId } = JSON.parse(body) as { counterpartyId: string };
        await new Promise<void>(resolve => window.setTimeout(resolve, Number(delay.value)));

        if (selectedScenario === "network") {
            throw new Error("Не удалось подключиться к CRM. Проверьте соединение и повторите запрос.");
        }
        let response: CounterpartyResponse;
        switch (selectedScenario) {
            case "not-found":
                response = { success: false, errorCode: "NOT_FOUND", errorMessage: "Контрагент с указанным ID не найден в CRM." };
                break;
            case "unavailable":
                response = { success: false, errorCode: "CRM_UNAVAILABLE", errorMessage: "CRM временно недоступна. Повторите запрос позже." };
                break;
            case "malformed":
                response = { success: true }; // Exercise the real service's response validation.
                break;
            case "partial":
                response = { success: true, counterparty: { id: counterpartyId, name: "ООО «Тестовый контрагент»" } };
                break;
            default:
                response = {
                    success: true,
                    counterparty: {
                        id: counterpartyId,
                        name: "ООО «Тестовый контрагент»",
                        inn: "7700000000",
                        kpp: "770001001",
                        legalAddress: "г. Москва, ул. Тестовая, д. 1",
                        phone: "+7 (000) 000-00-00",
                        email: "demo@example.test"
                    }
                };
        }
        return response as T;
    }
}, "/preview/mock-counterparty");

open.addEventListener("click", () => {
    if (document.querySelector("dialog")) return;
    const dialog = openCounterpartyDialog(service, "00000000-0000-0000-0000-000000000042", { preview: true });
    dialog.querySelector<HTMLInputElement>("input")!.value = "42";
});
