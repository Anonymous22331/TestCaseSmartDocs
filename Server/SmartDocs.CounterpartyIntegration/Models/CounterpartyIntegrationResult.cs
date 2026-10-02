namespace SmartDocs.CounterpartyIntegration.Models;

public sealed class CounterpartyIntegrationResult
{
    public bool Success { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public CounterpartyDto? Counterparty { get; init; }

    public static CounterpartyIntegrationResult Ok(CounterpartyDto counterparty) => new()
    {
        Success = true,
        Counterparty = counterparty
    };

    public static CounterpartyIntegrationResult Error(string errorCode, string errorMessage, CounterpartyDto? counterparty = null) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        Counterparty = counterparty
    };
}
