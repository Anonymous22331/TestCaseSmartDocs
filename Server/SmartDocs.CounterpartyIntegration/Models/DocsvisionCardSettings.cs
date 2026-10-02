namespace SmartDocs.CounterpartyIntegration.Models;

public sealed class DocsvisionCardSettings
{
    public string SectionAlias { get; init; } = "CounterpartyInfo";
    public string FieldAlias { get; init; } = "CounterpartyInfo";
    public Guid? SectionId { get; init; }
}
