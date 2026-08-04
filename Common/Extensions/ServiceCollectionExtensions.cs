using tms_template_net8.Integrations.ExternalApi;
using tms_template_net8.Integrations.ExternalApi.Interfaces;
using tms_template_net8.Services;

namespace tms_template_net8.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers outbound HTTP integrations (Core API, ACL, etc.).
    /// </summary>
    public static IServiceCollection AddExternalIntegrations(this IServiceCollection services)
    {
        services.AddHttpClient<IRemoteFileUploadClient, RemoteFileUploadClient>(ConfigureCoreApiHttpClient)
            .ConfigurePrimaryHttpMessageHandler(CreateCoreApiHttpHandler);
        return services;
    }

    private static HttpClientHandler CreateCoreApiHttpHandler(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var isDev = configuration.GetValue("TmsSdk:DevLive:IsDev", false)
            || string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Development",
                StringComparison.OrdinalIgnoreCase);

        var handler = new HttpClientHandler();
        if (isDev)
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        return handler;
    }

    private static void ConfigureCoreApiHttpClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["CoreApi:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("CoreApi:BaseUrl is not configured.");

        var appToken = configuration["CoreApi:AppToken"];
        if (string.IsNullOrWhiteSpace(appToken))
            throw new InvalidOperationException("CoreApi:AppToken is not configured.");

        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-App-Token", appToken);
    }

    /// <summary>
    /// Registers app-local services. Add new module/business services here instead of in Program.cs.
    /// </summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<IProductService, ProductService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IServerService, ServerService>();
        services.AddScoped<IApplicationGroupService, ApplicationGroupService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IApplicationDeploymentService, ApplicationDeploymentService>();
        services.AddScoped<IUptimeService, UptimeService>();
        services.AddScoped<IApplicationLogService, ApplicationLogService>();
        services.AddScoped<IAgentService, AgentService>();
        return services;
    }
}
