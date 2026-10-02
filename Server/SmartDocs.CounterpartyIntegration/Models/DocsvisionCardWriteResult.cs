namespace SmartDocs.CounterpartyIntegration.Models;

public sealed class DocsvisionCardWriteResult
{
    public bool Success { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;

    public static DocsvisionCardWriteResult Ok() => new() { Success = true };

    public static DocsvisionCardWriteResult Error(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
