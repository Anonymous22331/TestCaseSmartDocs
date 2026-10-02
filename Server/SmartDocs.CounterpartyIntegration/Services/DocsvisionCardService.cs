using System.Text.Json;
using DocsVision.Platform.ObjectManager;
using Microsoft.Extensions.Logging;
using SmartDocs.CounterpartyIntegration.Models;

namespace SmartDocs.CounterpartyIntegration.Services;

public sealed class DocsvisionCardService
{
    private readonly DocsvisionCardSettings _settings;
    private readonly ILogger<DocsvisionCardService> _logger;

    public DocsvisionCardService(DocsvisionCardSettings settings, ILogger<DocsvisionCardService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public Task<DocsvisionCardWriteResult> UpdateCounterpartyInfoAsync(
        UserSession session, Guid cardId, CounterpartyDto counterparty,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Update(session, cardId, counterparty));
    }

    private DocsvisionCardWriteResult Update(UserSession session, Guid cardId, CounterpartyDto counterparty)
    {
        if (cardId == Guid.Empty)
            return DocsvisionCardWriteResult.Error("INVALID_REQUEST", "Card ID is required.");

        try
        {
            var manager = session.CardManager;
            if (manager.GetCardState(cardId) != ObjectState.Existing)
                return MissingCard();

            CardData card;
            try
            {
                card = manager.GetCardData(cardId);
            }
            catch (ObjectManagerException) when (IsMissing(manager, cardId))
            {
                // The card may have been deleted between GetCardState and GetCardData.
                return MissingCard();
            }
            if (card is null) return MissingCard();

            var section = card.Sections.Cast<SectionData>().FirstOrDefault(item =>
                _settings.SectionId.HasValue
                    ? item.Id == _settings.SectionId.Value
                    : string.Equals(item.Type.Alias, _settings.SectionAlias, StringComparison.OrdinalIgnoreCase));
            if (section is null || !section.Fields.Contains(_settings.FieldAlias))
                return DocsvisionCardWriteResult.Error("CARD_FIELD_NOT_FOUND", "The configured card section or field does not exist.");

            var row = section.FirstRow ?? section.CreateRow();
            // ObjectManager persists a single field assignment immediately in normal mode.
            // Do not use ObjectModel.SaveObject with CardData, or invent a CardData.Save method.
            row[_settings.FieldAlias] = JsonSerializer.Serialize(counterparty);
            return DocsvisionCardWriteResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not update counterparty information in card {CardId}", cardId);
            return DocsvisionCardWriteResult.Error("CARD_UPDATE_FAILED", "Could not update the Docsvision card.");
        }
    }

    private static bool IsMissing(CardManager manager, Guid cardId)
    {
        try { return manager.GetCardState(cardId) != ObjectState.Existing; }
        catch { return false; } // Preserve the original error if the state cannot be read.
    }

    private static DocsvisionCardWriteResult MissingCard() =>
        DocsvisionCardWriteResult.Error("CARD_NOT_FOUND", "The Docsvision card was not found or has been deleted.");
}
