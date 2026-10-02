using DocsVision.Platform.WebClient;
using DocsVision.Platform.WebClient.Helpers;
using DocsVision.Platform.WebClient.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDocs.CounterpartyIntegration.Models;
using SmartDocs.CounterpartyIntegration.Services;

namespace SmartDocs.CounterpartyIntegration.ServerExtensions;

[Authorize]
[Route("api/Counterparty")]
public sealed class CounterpartyController : Controller
{
    private readonly ICurrentObjectContextProvider _contextProvider;
    private readonly CounterpartyIntegrationService _integration;

    public CounterpartyController(ICurrentObjectContextProvider contextProvider, CounterpartyIntegrationService integration)
    {
        _contextProvider = contextProvider;
        _integration = integration;
    }

    [HttpPost("GetCounterpartyForCard")]
    public async Task<ContentResult> GetCounterpartyForCard([FromBody] CounterpartyRequest? request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || request is null || request.CardId == Guid.Empty || string.IsNullOrWhiteSpace(request.CounterpartyId))
            return JsonResponse(CommonResponse.CreateSuccess(CounterpartyIntegrationResult.Error("INVALID_REQUEST", "Card ID and counterparty ID are required.")));

        var context = _contextProvider.GetOrCreateCurrentSessionContext();
        var result = await _integration.ProcessAsync(context.Session, request.CardId, request.CounterpartyId, cancellationToken);
        // RequestManager unwraps data and updates the current card's timestamp after a write.
        return result.Success
            ? JsonResponse(CommonResponse.CreateSuccess(context, request.CardId, result))
            : JsonResponse(CommonResponse.CreateSuccess(result));
    }

    private ContentResult JsonResponse(object value) => Content(JsonHelper.SerializeToJson(value), "application/json");
}

public sealed class CounterpartyRequest
{
    public Guid CardId { get; init; }
    public string CounterpartyId { get; init; } = string.Empty;
}
