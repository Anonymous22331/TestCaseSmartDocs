import { CustomButton } from "@docsvision/webclient/Platform/CustomButton";
import { $RequestManager } from "@docsvision/webclient/System/$RequestManager";
import { urlStore } from "@docsvision/webclient/System/UrlStore";
import { CounterpartyService } from "./counterparty";
import { openCounterpartyDialog } from "./dialog";

// Assign this registered handler to a CustomButton's click event in Layout Designer.
export function showCounterparty(sender: CustomButton): void {
    const endpoint = urlStore.urlResolver.resolveUrl("GetCounterpartyForCard", "Counterparty");
    const service = new CounterpartyService(sender.getService($RequestManager), endpoint);
    openCounterpartyDialog(service, sender.layout.cardInfo.id);
}
