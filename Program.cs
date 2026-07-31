using tms_template_net8.Common.Extensions;
using tms_template_net8.Common.Logging;
using tms_template_net8.Data;
using TMS.WebApp.Sdk.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFile(Path.Combine(builder.Environment.ContentRootPath, "Logs"));

builder.Services.AddControllers();
builder.Services.AddExternalIntegrations();

builder.Services.AddDataRepositories(builder.Configuration);
builder.Services.AddAppServices();
builder.Services.AddApiDocumentation();

if (!string.IsNullOrWhiteSpace(builder.Configuration["TmsSdk:ErrorLog:StoredProcedureName"]))
{
    builder.Services.UseSqlErrorLogger();
}

var app = builder.Build();

app.UseApiDocumentation();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "An unexpected error occurred."
            });
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapControllers();
app.Run();
