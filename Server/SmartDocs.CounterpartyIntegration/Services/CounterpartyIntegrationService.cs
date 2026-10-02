using SmartDocs.CounterpartyIntegration.Models;
using DocsVision.Platform.ObjectManager;

namespace SmartDocs.CounterpartyIntegration.Services;

public sealed class CounterpartyIntegrationService
{
    private readonly CounterpartyClient _counterpartyClient;
    private readonly DocsvisionCardService _docsvisionCardService;

    public CounterpartyIntegrationService(CounterpartyClient counterpartyClient, DocsvisionCardService docsvisionCardService)
    {
        _counterpartyClient = counterpartyClient;
        _docsvisionCardService = docsvisionCardService;
    }

    public async Task<CounterpartyIntegrationResult> ProcessAsync(
        UserSession session,
        Guid cardId,
        string counterpartyId,
        CancellationToken cancellationToken = default)
    {
        if (cardId == Guid.Empty)
        {
            return CounterpartyIntegrationResult.Error("INVALID_REQUEST", "Card ID is required.");
        }

        try
        {
            var counterparty = await _counterpartyClient.GetCounterpartyAsync(counterpartyId, cancellationToken);
            var docsvisionResult = await _docsvisionCardService.UpdateCounterpartyInfoAsync(
                session,
                cardId,
                counterparty,
                cancellationToken);

            if (!docsvisionResult.Success)
            {
                return CounterpartyIntegrationResult.Error(docsvisionResult.ErrorCode, docsvisionResult.ErrorMessage, counterparty);
            }

            return CounterpartyIntegrationResult.Ok(counterparty);
        }
        catch (CounterpartyClientException ex)
        {
            return CounterpartyIntegrationResult.Error(ex.ErrorCode, ex.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return CounterpartyIntegrationResult.Error("INTERNAL_ERROR", "An unexpected error occurred while processing the counterparty.");
        }
    }
}
