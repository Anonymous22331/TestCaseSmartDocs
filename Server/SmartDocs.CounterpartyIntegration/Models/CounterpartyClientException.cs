namespace SmartDocs.CounterpartyIntegration.Models;

public sealed class CounterpartyClientException : Exception
{
    public CounterpartyClientException(string errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
