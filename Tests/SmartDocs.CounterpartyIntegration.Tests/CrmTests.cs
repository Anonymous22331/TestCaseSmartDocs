using System.Net;
using System.Text.Json;
using SmartDocs.CounterpartyIntegration.Models;
using SmartDocs.CounterpartyIntegration.Services;
using Xunit;

namespace SmartDocs.CounterpartyIntegration.Tests;

public sealed class CrmTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"unexpected\":true}")]
    [InlineData("{\"id\":null,\"name\":null}")]
    [InlineData("{\"id\":\"42\",\"name\":\" \"}")]
    [InlineData("{\"id\":\"other\",\"name\":\"Company\"}")]
    [InlineData("{\"id\":42,\"name\":\"Company\"}")]
    [InlineData("null")]
    [InlineData("[")]
    public async Task Rejects_invalid_counterparty(string body)
    {
        using var http = new HttpClient(new StubHandler(_ => new(HttpStatusCode.OK) { Content = new StringContent(body) }));
        var error = await Assert.ThrowsAsync<CounterpartyClientException>(() => Client(http).GetCounterpartyAsync("42"));
        Assert.Equal("CRM_INVALID_RESPONSE", error.ErrorCode);
    }

    [Fact]
    public async Task Maps_crm_address_to_webclient_and_escapes_id()
    {
        using var http = new HttpClient(new StubHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://crm.test/api/counterparties/a%2Fb%20%3F", request.RequestUri!.AbsoluteUri);
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"id\":\"a/b ?\",\"name\":\"Company\",\"legal_address\":\"Address\"}") };
        }));
        var result = await Client(http).GetCounterpartyAsync("a/b ?");
        var json = JsonSerializer.SerializeToElement(result);
        Assert.Equal("Address", json.GetProperty("legalAddress").GetString());
        Assert.False(json.TryGetProperty("legal_address", out _));
    }

    [Theory]
    [InlineData("empty", "CRM_EMPTY_RESPONSE")]
    [InlineData("http", "CRM_HTTP_ERROR")]
    [InlineData("network", "CRM_UNAVAILABLE")]
    [InlineData("timeout", "CRM_TIMEOUT")]
    public async Task Reports_transport_errors(string mode, string expected)
    {
        using var http = new HttpClient(new StubHandler(_ => mode switch
        {
            "network" => throw new HttpRequestException("Offline"),
            "timeout" => throw new TaskCanceledException("Timeout"),
            "http" => new(HttpStatusCode.ServiceUnavailable),
            _ => new(HttpStatusCode.OK) { Content = new StringContent(" ") }
        }));
        var error = await Assert.ThrowsAsync<CounterpartyClientException>(() => Client(http).GetCounterpartyAsync("42"));
        Assert.Equal(expected, error.ErrorCode);
    }

    [Fact]
    public async Task Preserves_caller_cancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        using var http = new HttpClient(new StubHandler(_ => throw new OperationCanceledException(source.Token)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Client(http).GetCounterpartyAsync("42", source.Token));
    }

    [Fact]
    public void Applies_configured_timeout_to_default_httpclient()
    {
        using var http = new HttpClient();
        _ = Client(http);
        Assert.Equal(TimeSpan.FromSeconds(15), http.Timeout);
    }

    internal static CounterpartyClient Client(HttpClient http) => new(http, new CounterpartyClientOptions { BaseUrl = "https://crm.test" });
}

internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(respond(request));
}
