using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using DocsVision.Platform.WebClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartDocs.CounterpartyIntegration.ServerExtensions;
using Xunit;

namespace SmartDocs.CounterpartyIntegration.Tests;

public sealed class EndpointTests
{
    [Theory]
    [InlineData("POST", true, HttpStatusCode.OK)]
    [InlineData("POST", false, HttpStatusCode.Unauthorized)]
    [InlineData("GET", true, HttpStatusCode.MethodNotAllowed)]
    public async Task Registers_route_authorization_and_json_contract(string method, bool authenticated, HttpStatusCode expected)
    {
        var contextProvider = new Mock<ICurrentObjectContextProvider>(MockBehavior.Strict);
        using var server = new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
                services.AddSingleton(contextProvider.Object);
                new CounterpartyServerExtension(null!).InitializeServiceCollection(services);
                services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
                services.AddAuthorization();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            }));
        using var client = server.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/Counterparty/GetCounterpartyForCard");
        if (authenticated) request.Headers.Add("Authorization", "Test");
        request.Content = JsonContent.Create(new { cardId = Guid.Empty, counterpartyId = "42" });
        using var response = await client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
        {
            var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("data").GetProperty("success").GetBoolean());
            Assert.Equal("INVALID_REQUEST", body.GetProperty("data").GetProperty("errorCode").GetString());
        }
        contextProvider.VerifyNoOtherCalls();
    }
}

public sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
        Request.Headers.Authorization == "Test"
            ? AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Tester") }, Scheme.Name)), Scheme.Name))
            : AuthenticateResult.NoResult());
}
