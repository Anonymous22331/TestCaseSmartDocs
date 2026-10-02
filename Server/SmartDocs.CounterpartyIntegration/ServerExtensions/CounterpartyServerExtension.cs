using DocsVision.WebClient.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartDocs.CounterpartyIntegration.Models;
using SmartDocs.CounterpartyIntegration.Services;

namespace SmartDocs.CounterpartyIntegration.ServerExtensions;

public sealed class CounterpartyServerExtension : WebClientExtension
{
    public CounterpartyServerExtension(IServiceProvider serviceProvider) { }
    public override string ExtensionName => "SmartDocs.CounterpartyIntegration";
    public override Version ExtensionVersion => new(1, 0, 0);

    public override void InitializeServiceCollection(IServiceCollection services)
    {
        services.AddSingleton(provider => provider.GetRequiredService<IConfiguration>()
            .GetSection("CounterpartyIntegration:Crm").Get<CounterpartyClientOptions>() ?? new());
        services.AddSingleton(provider => provider.GetRequiredService<IConfiguration>()
            .GetSection("CounterpartyIntegration:Card").Get<DocsvisionCardSettings>() ?? new());
        services.AddHttpClient<CounterpartyClient>();
        services.AddScoped<DocsvisionCardService>();
        services.AddScoped<CounterpartyIntegrationService>();
        services.AddControllers().AddApplicationPart(typeof(CounterpartyServerExtension).Assembly);
    }
}
