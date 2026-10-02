using System.Text.Json.Serialization;

namespace SmartDocs.CounterpartyIntegration.Models;

// CRM and WebClient use different address property names.
internal sealed class CrmCounterpartyResponse
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Inn { get; init; }
    public string? Kpp { get; init; }
    [JsonPropertyName("legal_address")]
    public string? LegalAddress { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
}
