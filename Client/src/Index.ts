import { extensionManager } from "@docsvision/webclient/System/ExtensionManager";
import * as CounterpartyHandlers from "./webclient";

extensionManager.registerExtension({
    name: "SmartDocs.CounterpartyIntegration",
    version: "1.0.0",
    globalEventHandlers: [CounterpartyHandlers]
});
