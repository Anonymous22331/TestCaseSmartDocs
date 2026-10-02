using System.Net.Http;
using System.Text.Json;
using SmartDocs.CounterpartyIntegration.Models;

namespace SmartDocs.CounterpartyIntegration.Services;

public sealed class CounterpartyClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly CounterpartyClientOptions _options;

    public CounterpartyClient(HttpClient httpClient, CounterpartyClientOptions options)
    {
        _httpClient = httpClient;
        _options = options;

        _httpClient.Timeout = _options.Timeout;
    }

    public async Task<CounterpartyDto> GetCounterpartyAsync(string counterpartyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(counterpartyId))
        {
            throw new CounterpartyClientException("INVALID_REQUEST", "Counterparty ID is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new CounterpartyClientException("CRM_UNAVAILABLE", "CRM base URL is not configured.");
        }

        var normalizedId = counterpartyId.Trim();
        var requestUri = new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), $"api/counterparties/{Uri.EscapeDataString(normalizedId)}");

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new CounterpartyClientException(
                    "CRM_HTTP_ERROR",
                    $"CRM returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
            {
                throw new CounterpartyClientException("CRM_EMPTY_RESPONSE", "CRM returned an empty response.");
            }

            try
            {
                var model = JsonSerializer.Deserialize<CrmCounterpartyResponse>(payload, JsonOptions);
                if (model is null || string.IsNullOrWhiteSpace(model.Id) || string.IsNullOrWhiteSpace(model.Name)
                    || !string.Equals(model.Id, normalizedId, StringComparison.Ordinal))
                {
                    throw new CounterpartyClientException("CRM_INVALID_RESPONSE", "CRM response did not contain a valid counterparty payload.");
                }

                return new CounterpartyDto
                {
                    Id = model.Id, Name = model.Name, Inn = model.Inn, Kpp = model.Kpp,
                    LegalAddress = model.LegalAddress, Phone = model.Phone, Email = model.Email
                };
            }
            catch (JsonException ex)
            {
                throw new CounterpartyClientException("CRM_INVALID_RESPONSE", "CRM returned JSON in an unexpected format.", ex);
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CounterpartyClientException("CRM_TIMEOUT", "CRM request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new CounterpartyClientException("CRM_UNAVAILABLE", "CRM API is unavailable.", ex);
        }
    }
}
