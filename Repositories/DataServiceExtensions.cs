using TMS.WebApp.Sdk.DependencyInjection;
using tms_template_net8.Data.Repositories;
namespace tms_template_net8.Data;
public static class DataServiceExtensions
{
    /// <summary>
    /// Registers TMS SDK SQL access (<see cref="TMS.WebApp.Sdk.Data.Sql.ISqlExecutor"/>) and repositories.
    /// Connection resolution:
    /// <list type="bullet">
    ///   <item>When <c>TmsSdk:ConnectionString:UseRemoteResolver</c> is true and RemoteResolverUrl is set, strings are fetched remotely.</item>
    ///   <item>Otherwise, strings are read from <c>ConnectionStrings:{DefaultName}</c> (defaults to <c>Default</c>).</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddDataRepositories(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTmsWebAppSdk(configuration, opts =>
        {
            if (string.IsNullOrWhiteSpace(opts.ConnectionString.DefaultName))
                opts.ConnectionString.DefaultName = "Default";
        });
        var useRemote = configuration.GetValue("TmsSdk:ConnectionString:UseRemoteResolver", false);
        if (useRemote && !string.IsNullOrWhiteSpace(configuration["TmsSdk:ConnectionString:RemoteResolverUrl"]))
            services.UseRemoteAclConnectionProvider();
        // Product sample remains in-memory; platform repos use SQL via ISqlExecutor.
        services.AddSingleton<IProductRepository, ProductRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IServerRepository, ServerRepository>();
        services.AddScoped<IApplicationGroupRepository, ApplicationGroupRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IApplicationUptimeLogRepository, ApplicationUptimeLogRepository>();
        services.AddScoped<IServerMetricsRepository, ServerMetricsRepository>();
        services.AddScoped<IApplicationDeploymentRepository, ApplicationDeploymentRepository>();
        services.AddScoped<IApplicationLogRepository, ApplicationLogRepository>();
        services.AddScoped<IApplicationLogChunkRepository, ApplicationLogChunkRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();
        return services;
    }
}
