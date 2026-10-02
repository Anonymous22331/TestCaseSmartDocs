using System.Collections;
using System.Text.Json;
using DocsVision.Platform.ObjectManager;
using DocsVision.Platform.ObjectManager.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartDocs.CounterpartyIntegration.Models;
using SmartDocs.CounterpartyIntegration.Services;
using Xunit;

namespace SmartDocs.CounterpartyIntegration.Tests;

public sealed class CardTests
{
    private readonly Guid _cardId = Guid.NewGuid();
    private readonly Guid _sectionId = Guid.NewGuid();
    private readonly Mock<UserSession> _session = new();
    private readonly Mock<CardManager> _manager = new();
    private readonly Mock<CardData> _card = new();
    private readonly Mock<SectionData> _section = new();
    private readonly Mock<RowData> _row = new();
    private readonly Mock<RowFieldCollection> _fields = new();
    private readonly DocsvisionCardService _service;
    private readonly TestLogger _logger = new();

    public CardTests()
    {
        _session.SetupGet(s => s.CardManager).Returns(_manager.Object);
        _manager.Setup(m => m.GetCardState(_cardId)).Returns(ObjectState.Existing);
        _manager.Setup(m => m.GetCardData(_cardId, It.IsAny<bool>())).Returns(_card.Object);
        var sections = new Mock<SectionDataCollection>();
        sections.As<IEnumerable>().Setup(s => s.GetEnumerator()).Returns(() => new[] { _section.Object }.GetEnumerator());
        sections.As<IEnumerable<SectionData>>().Setup(s => s.GetEnumerator()).Returns(() => ((IEnumerable<SectionData>)new[] { _section.Object }).GetEnumerator());
        _card.SetupGet(c => c.Sections).Returns(sections.Object);
        _section.SetupGet(s => s.Id).Returns(_sectionId);
        var metadata = new Mock<CardSection>();
        metadata.SetupGet(s => s.Alias).Returns("CounterpartyInfo");
        _section.SetupGet(s => s.Type).Returns(metadata.Object);
        _section.SetupGet(s => s.Fields).Returns(_fields.Object);
        _fields.Setup(f => f.Contains("CounterpartyInfo")).Returns(true);
        _section.SetupGet(s => s.FirstRow).Returns(_row.Object);
        _service = new(new DocsvisionCardSettings { SectionId = _sectionId }, _logger);
    }

    private Task<DocsvisionCardWriteResult> Write(DocsvisionCardService? service = null) => (service ?? _service).UpdateCounterpartyInfoAsync(
        _session.Object, _cardId, new CounterpartyDto { Id = "42", Name = "Company", LegalAddress = "Address" });

    [Fact]
    public async Task Writes_json_to_actual_sdk_row_indexer()
    {
        string? saved = null;
        _row.SetupSet(r => r["CounterpartyInfo"] = It.IsAny<object>()).Callback<string, object>((_, value) => saved = (string)value);
        Assert.True((await Write()).Success, _logger.Error?.ToString());
        Assert.Equal("Address", JsonDocument.Parse(saved!).RootElement.GetProperty("legalAddress").GetString());
    }

    [Fact]
    public async Task Resolves_alias_from_section_metadata()
    {
        var service = new DocsvisionCardService(new(), NullLogger<DocsvisionCardService>.Instance);
        Assert.True((await Write(service)).Success);
        _row.VerifySet(r => r["CounterpartyInfo"] = It.IsAny<object>(), Times.Once);
    }

    [Theory]
    [InlineData(ObjectState.NotExisting)]
    [InlineData(ObjectState.Deleted)]
    public async Task Reports_missing_or_deleted_card(ObjectState state)
    {
        _manager.Setup(m => m.GetCardState(_cardId)).Returns(state);
        Assert.Equal("CARD_NOT_FOUND", (await Write()).ErrorCode);
        _manager.Verify(m => m.GetCardData(_cardId, It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Handles_card_deleted_during_lookup()
    {
        _manager.SetupSequence(m => m.GetCardState(_cardId)).Returns(ObjectState.Existing).Returns(ObjectState.NotExisting);
        _manager.Setup(m => m.GetCardData(_cardId, It.IsAny<bool>())).Throws(new ObjectManagerException("Missing"));
        Assert.Equal("CARD_NOT_FOUND", (await Write()).ErrorCode);
    }

    [Fact]
    public async Task Does_not_disguise_permission_error_as_missing_card()
    {
        _manager.Setup(m => m.GetCardData(_cardId, It.IsAny<bool>())).Throws(new ObjectManagerException("Sensitive permission details"));
        var result = await Write();
        Assert.Equal("CARD_UPDATE_FAILED", result.ErrorCode);
        Assert.DoesNotContain("Sensitive", result.ErrorMessage);
    }

    [Fact]
    public async Task Rejects_missing_field_without_writing()
    {
        _fields.Setup(f => f.Contains("CounterpartyInfo")).Returns(false);
        Assert.Equal("CARD_FIELD_NOT_FOUND", (await Write()).ErrorCode);
        _row.VerifySet(r => r[It.IsAny<string>()] = It.IsAny<object>(), Times.Never);
    }

    [Fact]
    public async Task Creates_first_row_in_empty_section()
    {
        _section.SetupGet(s => s.FirstRow).Returns((RowData)null!);
        _section.Setup(s => s.CreateRow(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(_row.Object);
        Assert.True((await Write()).Success, _logger.Error?.ToString());
        _row.VerifySet(r => r["CounterpartyInfo"] = It.IsAny<object>(), Times.Once);
    }
}

internal sealed class TestLogger : ILogger<DocsvisionCardService>
{
    public Exception? Error { get; private set; }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Error = exception;
}
