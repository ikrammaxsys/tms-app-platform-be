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
        services.AddScoped<IUptimeService, UptimeService>();
        return services;
    }
}
