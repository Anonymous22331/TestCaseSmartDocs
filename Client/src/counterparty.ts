import type { IRequestManager } from "@docsvision/webclient/System/$RequestManager";

export interface Counterparty {
    id: string;
    name: string;
    inn?: string;
    kpp?: string;
    legalAddress?: string;
    phone?: string;
    email?: string;
}

export interface CounterpartyResponse {
    success: boolean;
    counterparty?: Counterparty;
    errorCode?: string;
    errorMessage?: string;
}

export class CounterpartyService {
    constructor(private readonly requestManager: Pick<IRequestManager, "post">, private readonly endpoint: string) {}

    public async getCounterparty(cardId: string, counterpartyId: string): Promise<CounterpartyResponse> {
        if (!cardId || !counterpartyId.trim()) {
            return {
                success: false,
                errorCode: "INVALID_REQUEST",
                errorMessage: "Card ID and counterparty ID are required."
            };
        }

        const result = await this.requestManager.post<CounterpartyResponse>(this.endpoint,
            JSON.stringify({ cardId, counterpartyId: counterpartyId.trim() }));
        if (!result || typeof result.success !== "boolean" || (result.success &&
            (!result.counterparty || typeof result.counterparty.id !== "string" || !result.counterparty.id.trim() ||
                typeof result.counterparty.name !== "string" || !result.counterparty.name.trim()))) {
            throw new Error("Сервер вернул некорректные данные контрагента.");
        }
        return result;
    }
}

export interface CounterpartyViewState {
    loading: boolean;
    counterparty: Counterparty | null;
    error: string | null;
}

export class CounterpartyWidget {
    private state: CounterpartyViewState = {
        loading: false,
        counterparty: null,
        error: null
    };

    constructor(
        private readonly service: CounterpartyService,
        private readonly cardId: string,
        private readonly counterpartyId: string,
        private readonly render: (state: CounterpartyViewState) => void
    ) {}

    public async load(): Promise<void> {
        if (this.state.loading) {
            return;
        }

        this.state = {
            loading: true,
            counterparty: null,
            error: null
        };
        this.render(this.state);

        try {
            const result = await this.service.getCounterparty(this.cardId, this.counterpartyId);

            if (!result.success) {
                this.state = {
                    loading: false,
                    counterparty: null,
                    error: result.errorMessage ?? "The counterparty request failed."
                };
                this.render(this.state);
                return;
            }

            this.state = {
                loading: false,
                counterparty: result.counterparty ?? null,
                error: null
            };
            this.render(this.state);
        }
        catch (error: unknown) {
            const message = error instanceof Error ? error.message : "Unexpected error while loading counterparty data.";
            this.state = {
                loading: false,
                counterparty: null,
                error: message
            };
            this.render(this.state);
        }
    }
}
