namespace SmartDocs.CounterpartyIntegration.Models;

public sealed class CounterpartyClientOptions
{
    public string BaseUrl { get; init; } = string.Empty;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(15);
}
