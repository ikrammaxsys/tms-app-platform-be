using tms_template_net8.Integrations.ExternalApi;
using tms_template_net8.Services;

namespace tms_template_net8.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers app-local services. Add new module/business services here instead of in Program.cs.
    /// </summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<IProductService, ProductService>();
        services.AddScoped<IServerService, ServerService>();
        services.AddScoped<IApplicationGroupService, ApplicationGroupService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IApplicationDeploymentService, ApplicationDeploymentService>();
        return services;
    }

    /// <summary>
    /// Registers HTTP clients and other external system integrations.
    /// </summary>
    public static IServiceCollection AddExternalIntegrations(this IServiceCollection services)
    {
        services.AddHttpClient<IACLService, ACLService>((sp, client) =>
        {
            var baseUrl = sp.GetRequiredService<IConfiguration>()["Dsp:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Configuration 'Dsp:BaseUrl' is required.");

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddHttpClient<ICoreAPIService, CoreAPIService>((sp, client) =>
        {
            var baseUrl = sp.GetRequiredService<IConfiguration>()["CoreApi:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Configuration 'CoreApi:BaseUrl' is required.");

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        return services;
    }
}
