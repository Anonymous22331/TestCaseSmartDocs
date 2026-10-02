using System.Text.Json.Serialization;

namespace SmartDocs.CounterpartyIntegration.Models;

public sealed class CounterpartyDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("inn")]
    public string? Inn { get; init; }

    [JsonPropertyName("kpp")]
    public string? Kpp { get; init; }

    [JsonPropertyName("legalAddress")]
    public string? LegalAddress { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }
}
