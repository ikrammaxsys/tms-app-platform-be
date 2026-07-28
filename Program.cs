using tms_template_net8.Auth;
using tms_template_net8.Auth.Security;
using tms_template_net8.Common.Extensions;
using tms_template_net8.Common.Logging;
using tms_template_net8.Data;
using TMS.WebApp.Sdk.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
await RsaKeyLoader.SyncPublicPemFromAuthAsync(builder.Configuration, builder.Environment);

// Add file logger 
builder.Logging.AddFile(Path.Combine(builder.Environment.ContentRootPath, "Logs"));

var mvcBuilder = builder.Services.AddControllersWithViews();
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}
builder.Services.AddHttpClient();

builder.Services.AddAuthAndAcl(builder.Configuration, builder.Environment);
builder.Services.AddDataRepositories(builder.Configuration);
builder.Services.AddExternalIntegrations();
builder.Services.AddAppServices();
builder.Services.AddApiDocumentation();

// Optional: log SDK errors through the configured SQL stored procedure.
if (!string.IsNullOrWhiteSpace(builder.Configuration["TmsSdk:ErrorLog:StoredProcedureName"]))
{
    builder.Services.UseSqlErrorLogger();
}

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

// Swagger / OpenAPI UI available at /swagger.
app.UseApiDocumentation();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
// app.UseRootAclRedirect();
// app.UseAccessTokenValidation();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();
app.Run();
