using System.Reflection;
using Microsoft.OpenApi.Models;

namespace tms_template_net8.Common.Extensions;

/// <summary>
/// Registers OpenAPI / Swagger generation for the JSON API controllers.
/// </summary>
public static class SwaggerServiceExtensions
{
    public const string DocName = "v1";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocName, new OpenApiInfo
            {
                Title = "TMS Dev Platform API",
                Version = "v1",
                Description = "REST API for the TMS Dev Platform (applications, application groups, servers, products, and uptime)."
            });

            // Include XML summary comments when the documentation file is present.
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        });

        return services;
    }

    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/{DocName}/swagger.json", "TMS Dev Platform API v1");
            options.DocumentTitle = "TMS Dev Platform API";
        });
        return app;
    }
}
