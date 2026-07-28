# TMS Template (.NET 8)

ASP.NET Core MVC template for TMS subsystem projects with:

- TMS UI Core components for layout and page widgets
- TMS Core SDK/Common module integration points
- TMS Core API connectivity scaffold
- ACL V2 flow for authentication and authorization checks

This template gives a ready-to-use shell (sidebar, topbar, session handling, ACL entry flow) plus an example `ProductManagement` module that demonstrates list/create/detail/edit patterns.

## Tech Stack

- .NET 8 (`Microsoft.NET.Sdk.Web`)
- ASP.NET Core MVC + Web API
- Session + JWT bearer authentication
- TMS UI Core Web Components (loaded from UI Foundation loader)
- Package reference: `AuthACL.CentralAuth`

## Project Structure

```
.
├── Program.cs                              # Thin bootstrap; pipeline + DI extensions only
├── appsettings.json
├── appsettings.Development.json
├── tms-template-net8.csproj
├── tms-template-net8.sln
├── DEVELOPMENT_POLICY.md                   # Team conventions and layering rules
│
├── Auth/                                   # All auth/ACL code in one folder
│   ├── AuthServiceExtensions.cs              # AddAuthAndAcl(), UseAccessTokenValidation(), UseRootAclRedirect()
│   ├── Models/                               # ACL session + token DTOs
│   │   ├── AccessRight.cs
│   │   ├── AclTokenRequest.cs
│   │   ├── UserAclData.cs
│   │   └── UserDetail.cs
│   ├── Security/
│   │   ├── PageAccess.cs                     # [RequirePageAccess] attribute + authorization filter
│   │   └── RsaKeyLoader.cs                   # JWT key loading + public.pem sync
│   └── Services/
│       ├── AclCheckingService.cs             # auth-code exchange + verify helpers
│       ├── AuthTokenRefreshService.cs        # token refresh against Dsp:BaseUrl
│       ├── TokenService.cs                   # ITokenService + AuthTokenValidationKind
│       └── UserAccessControlService.cs       # loads + caches UserAclData in session
│
├── Middleware/                             # Custom ASP.NET middleware
│   └── AccessTokenValidationMiddleware.cs    # per-request token gate + refresh
│
├── Common/                                 # Shared app infrastructure
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs    # AddAppServices() + AddExternalIntegrations()
│   ├── Helpers/                              # shared helpers (placeholder)
│   └── Logging/
│       └── FileLoggerExtension.cs            # file logging provider wiring
│
├── Repositories/                           # TMS SDK SQL access + repository layer
│   ├── DataServiceExtensions.cs              # AddDataRepositories() — SDK + repo registration
│   ├── ProductRepository.cs                  # in-memory sample store (swap for ISqlExecutor)
│   └── Interfaces/
│       └── IProductRepository.cs
│
├── Controllers/
│   ├── Web/                                # MVC pages (return Razor views)
│   │   ├── ACLCheckingController.cs          # ACL gate: callback, verify, logout
│   │   ├── HomeController.cs
│   │   └── ProductManagementController.cs
│   └── Api/                                # JSON endpoints
│       ├── IndexController.cs                # GET /index health
│       └── ProductsController.cs
│
├── Services/                               # App-local business services
│   ├── ProductService.cs                     # sample CRUD; delegates to ProductRepository
│   └── Interfaces/                           # app-local I*Service contracts
│
├── Integrations/                           # Outbound HTTP integrations only
│   └── ExternalApi/
│       ├── ACLService.cs                     # DSP/ACL HTTP integration example
│       ├── CoreAPIService.cs                 # Core API HTTP integration example
│       └── Interfaces/                       # IACLService, ICoreAPIService
│
├── Models/                                 # App DTOs and view models (not auth/ACL)
│   ├── DTOs/
│   │   ├── ApiResponse.cs
│   │   ├── CoreApiResponse.cs
│   │   └── Product/
│   │       ├── ProductItem.cs
│   │       └── ProductUpsertRequest.cs
│   └── ViewModels/
│       ├── ConfirmationModalViewModel.cs
│       └── ErrorViewModel.cs
│
├── Views/                                  # MVC Razor views
│   ├── Shared/                               # _Layout, _Sidebar, _Topbar, _ConfirmationModal
│   ├── Home/
│   ├── ACLChecking/
│   └── ProductManagement/
│
├── wwwroot/                                # Static web assets
│   ├── js/                                   # apiClient, apiRoutes, notifications, …
│   ├── css/
│   ├── data/                                 # sidebar-items.json, status-options.json
│   ├── fontawesome/                          # icon fonts
│   ├── lib/                                  # third-party (bootstrap, jquery, …)
│   └── ProductManagement/                    # per-module JS/CSS
│       ├── js/
│       └── css/
│
├── templates/
│   └── tms-module/                         # Local dotnet new module scaffold
│
├── keys/                                   # RSA PEM (synced from auth API on startup via RsaKeyLoader)
└── Logs/                                   # File log output (created at runtime)
```

## What Is Inside

- `Program.cs`
  - Thin bootstrap. All DI is delegated to small extension methods:
    `services.AddAuthAndAcl(...)` (auth + ACL gate, in `Auth/`)
    `services.AddDataRepositories(...)` (TMS SDK SQL + repositories, in `Repositories/`)
    `services.AddExternalIntegrations()` (HTTP clients, in `Common/Extensions/`)
    and `services.AddAppServices()` (app-local services, in `Common/Extensions/`).
  - Registers TMS Core SDK via `AddDataRepositories()` → `AddTmsWebAppSdk(...)`, with optional:
    - default SQL connection-name fallback to `ConnectionStrings:Default`
    - remote connection-string resolution (`UseRemoteAclConnectionProvider()`)
    - SQL stored-procedure error logging (`UseSqlErrorLogger()`)
  - Pipeline composes the ACL bootstrap redirect (`/` → `/ACLChecking`)
    and the access-token validation middleware.
- `Auth/` (all auth/ACL code in one folder)
  - Config keys read from `Jwt` and `Auth` sections in `appsettings.json`
  - JWT key loading (`Security/RsaKeyLoader`) and validation (`Services/TokenService`)
  - Auth-code exchange + verify (`Services/AclCheckingService`)
  - Token refresh service (`Services/AuthTokenRefreshService`)
  - Page-level access control (`Security/PageAccess.cs`: `RequirePageAccessAttribute` + `PageAccessAuthorizationFilter`)
  - ACL session cache (`Services/UserAccessControlService`)
  - DI + pipeline wiring (`AuthServiceExtensions`): `AddAuthAndAcl(...)`, `UseAccessTokenValidation()`, `UseRootAclRedirect()`
- `Middleware/`
  - `AccessTokenValidationMiddleware`: per-request token gate + refresh (uses the `Auth/` services)
- `Common/`
  - `Extensions/ServiceCollectionExtensions.cs`: `AddAppServices()` + `AddExternalIntegrations()`
  - `Logging/FileLoggerExtension.cs`: file logging to `Logs/`
- `Repositories/`
  - `DataServiceExtensions.AddDataRepositories()`: TMS SDK registration + repository DI
  - `ProductRepository`: in-memory sample store behind `IProductRepository`
- `Controllers/Web/ACLCheckingController.cs`
  - Entry point for ACL callback/query parameters
  - Auth code exchange flow
  - Token verification flow (loads roles + access controls into session)
  - Session population (`gstrUserID`, `gstrUserName`, `UserAclData`)
  - Logout and session cleanup flow
- `Services/`
  - `ProductService`: sample CRUD service; holds business logic and delegates persistence to `ProductRepository`
  - registered via `Common/Extensions/ServiceCollectionExtensions.AddAppServices()`
- `Integrations/ExternalApi/`
  - `ACLService`: DSP/ACL user lookup (template scaffolding example)
  - `CoreAPIService`: sample call to TMS Core API (`/api/v1/status`)
  - registered via `Common/Extensions/ServiceCollectionExtensions.AddExternalIntegrations()`
- `Controllers/Web` and `Controllers/Api`
  - MVC pages + API endpoints
  - Example product CRUD API in `api/products`
- `Views/Shared`
  - Main layout with `<ui-sidebar>` and `<ui-topbar>`
  - Reusable confirmation modal partial
- `wwwroot/`
  - Shared JS (`apiClient`, route mapping, nav helper, notifications)
  - Module JS under `wwwroot/ProductManagement/js`
  - Sidebar data and static dropdown options in `wwwroot/data`

## Architecture Overview

1. External app redirects to `ACLChecking` with query params (for example `ID_ACL_USER`, `auth-code`).
2. `ACLCheckingController` exchanges auth code (when present), sets token cookies, and verifies token with auth API.
3. On success, user session values are set and user is redirected to `Home/Index`.
4. `AccessTokenValidationMiddleware` protects non-API pages:
   - Reads access token from cookie/query
   - Validates JWT
   - Attempts refresh if token expired and refresh token exists
   - Redirects to `Home/SessionExpired` if invalid
5. `SessionExpired` view redirects back to configured DSP base URL.

## Configuration

Primary runtime settings are in `appsettings.json`. The `Jwt` and `Auth` sections
are bound to strongly-typed `JwtOptions` and `AuthOptions` (defaults shown in
parentheses; defaults are baked into the option classes so missing keys are fine):

- `Jwt` → `JwtOptions`
  - `RsaKeyPath` — required
  - `Issuer` (`"authapi"`)
  - `Audience` (`"authapi-client"`)
- `Auth` → `AuthOptions`
  - `BaseUrl` — base URL of the auth (token-issuing) service. Used by `RsaKeyLoader` (public.pem sync) and the auth-code exchange.
  - `ExchangeAuthCodeUrl` — path or absolute URL for the auth-code exchange (against `Auth:BaseUrl`)
  - `RefreshTokenApiUrl` (`"/api/auth/refresh-token"`) — path used for token refresh (against `Dsp:BaseUrl`)
  - `UserRolesAndAccessUrl` (`"/api/users/{idAclUser}/role-access"`) — endpoint that returns `{ data: { user, roles, accessControls } }` for the page-access filter. Supports `{idAclUser}` placeholder. A path is combined with `Dsp:BaseUrl`; an absolute URL is used as-is.
  - `AccessTokenStorageKey` (`"authacl_access_token"`)
  - `RefreshTokenStorageKey` (`"authacl_refresh_token"`)
  - `RefreshTokenRequestUsesGrantType` (`false`) — when true, refresh body is `{ grantType, refreshToken }`; otherwise `{ refreshToken }`
- `Dsp`
  - `BaseUrl` — base URL of the DSP/ACL service (a separate backend from `Auth:BaseUrl`). Used for token refresh, the roles/access load, and logout/session-expired redirects.
- `UiFoundation`
  - `BaseUrl` (TMS UI web component loader base URL)
- `CoreApi`
  - `BaseUrl` (TMS Core API base URL)
- `ConnectionStrings`
  - `Default` (fallback SQL Server connection string used when no logical connection name is provided)
- `TmsSdk`
  - `ConnectionString:DefaultName` (logical DB connection name for `ISqlExecutor`)
  - `ConnectionString:RemoteResolverUrl` (optional; enables remote connection-string provider)
  - `ErrorLog:StoredProcedureName` (optional; enables SQL stored-procedure error logger)
  - `ErrorLog:ConnectionName` (optional; connection-name override for error logging)

Optional/used by views and middleware:

- `App:SystemName` for title/footer branding

> Sub-path hosting (app mounted under a base path) is handled automatically via `Request.PathBase` in the middleware, filter, and ACL redirects — no extra config needed.

> `appsettings.Development.json` currently keeps these sections commented. Add/override values there for local development.

## How To Run

1. Install .NET 8 SDK.
2. Configure `appsettings.json` (or `appsettings.Development.json`) for your environment:
   - Auth API URL
   - DSP/ACL base URL
   - Core API base URL
   - JWT public key path and issuer/audience
3. Restore and run:

```bash
dotnet restore
dotnet run
```

Default local URL is shown in console output.

## How To Use This Template For A New Module

Use the local `dotnet new` item template to scaffold a module. The generated module follows a **controller → service → repository** flow: the controller handles HTTP and `ApiResponse` shaping, the service holds business logic, and the repository owns data access. The generated Razor pages follow the same UI component styling as `ProductManagement`, but the controller/service/repository code stays intentionally light so each module can add its own logic.

### 1. Install the local module template

From the project root:

```bash
dotnet new install ./templates/tms-module --force
```

### 2. Generate a module

Example:

```bash
dotnet new tms-module -n Order
```

This creates:

```text
Controllers/Api/OrderController.cs
Controllers/Web/OrderController.cs
Models/DTOs/Order/OrderItem.cs
Models/DTOs/Order/OrderRequest.cs
Models/DTOs/Order/OrderResponse.cs
Services/Interfaces/IOrderService.cs
Services/OrderService.cs
Repositories/IOrderRepository.cs
Repositories/OrderRepository.cs
Views/Order/Index.cshtml
Views/Order/Create.cshtml
Views/Order/Edit.cshtml
Views/Order/Detail.cshtml
Views/Order/Delete.cshtml
wwwroot/Order/js/index.js
wwwroot/Order/js/create.js
wwwroot/Order/js/edit.js
wwwroot/Order/js/detail.js
wwwroot/Order/js/delete.js
```

Preview generated files without writing them:

```bash
dotnet new tms-module -n Order --dry-run
```

### 3. Register the service and repository

Register the generated repository in `Repositories/DataServiceExtensions.cs` inside `AddDataRepositories(...)`:

```csharp
services.AddScoped<IOrderRepository, OrderRepository>();
```

Register the generated service in `Common/Extensions/ServiceCollectionExtensions.cs` inside `AddAppServices(...)`:

```csharp
services.AddScoped<IOrderService, OrderService>();
```

The service is constructor-injected with the repository, so both must be registered for the module to resolve. Use `AddScoped` for request-based/data-backed services. Use `AddSingleton` only for stateless or in-memory services that are safe to share across requests.

### 4. Fill in module logic

- Add real request/response properties in `Models/DTOs/<Module>/`.
- Implement data access (SDK/SQL) in `Repositories/<Module>Repository.cs` — the stub methods just return empty/false.
- Put business logic (validation, mapping, defaults) in `Services/<Module>Service.cs`; it delegates persistence to the repository.
- Keep web controller actions thin; they should usually return views only.
- Keep API controller actions focused on calling services and returning `ApiResponse` success/failure responses.
- Add page behavior in `wwwroot/<Module>/js/`.

The layering keeps responsibilities separate: swap the repository body for a real data store later without touching the service or controller. See `Services/ProductService.cs` and `Repositories/ProductRepository.cs` for a worked example.

### 5. Add navigation and access control

- Update `wwwroot/data/sidebar-items.json` with the new module menu entry.
- Add `[RequirePageAccess(...)]` to the web controller/actions when the ACL resource name is known.

## TMS Components Integration Notes

### Documentation Links

- TMS Core SDK Docs: [http://10.230.8.170/core-sdk-docs/](http://10.230.8.170/core-sdk-docs/)
- TMS UI Core Docs: [http://10.230.8.170/UiFoundationdocs/](http://10.230.8.170/UiFoundationdocs/)

### TMS UI Core

The layout uses TMS UI web components:

- `<ui-sidebar>` in `_Sidebar.cshtml`
- `<ui-topbar>` in `_Topbar.cshtml`
- `<ui-page-section>`, `<ui-form-card>`, `<ui-form-content>`, `<ui-text-input>`, `<ui-dropdown>`, `<ui-button>`, `<ui-datatable>` in module pages

These are loaded via the UI Foundation loader script in `Views/Shared/_Layout.cshtml`. Configure `UiFoundation:BaseUrl` for your UI Core environment.

### TMS Core SDK / Common Module

`AuthACL.CentralAuth` package is installed and used in ACL/auth flows.  
For additional TMS common modules, follow the same pattern:

1. Add package reference in `.csproj`
2. For external HTTP/API clients, place the implementation in `Integrations/ExternalApi/`; for data access, use `Repositories/`
3. Register repositories in `Repositories/DataServiceExtensions.cs` (`AddDataRepositories`); register HTTP clients in `Common/Extensions/ServiceCollectionExtensions.cs` (`AddExternalIntegrations`)
4. Keep app-local business services in `Services/` and register them through `AddAppServices` in `Common/Extensions/ServiceCollectionExtensions.cs`

#### SQL data access with `ISqlExecutor`

`ISqlExecutor`, `ISettingService`, and `IDropdownService` are registered by `AddDataRepositories()` → `AddTmsWebAppSdk(...)`.

- For stored procedures, pass `IEnumerable<SqlParameter>` and `CommandType.StoredProcedure`
- For Dapper-style queries, use `QueryAsync<T>` / `QuerySingleAsync<T>` with an anonymous object or POCO parameter object
- `connectionName` is optional:
  - when omitted, SDK uses `TmsSdk:ConnectionString:DefaultName`
  - when remote resolver is enabled, the name is resolved via `TmsSdk:ConnectionString:RemoteResolverUrl`
  - otherwise it resolves from `ConnectionStrings:<name>`

See `Repositories/DataServiceExtensions.cs` and `Repositories/ProductRepository.cs` for where to wire SQL-backed repositories.

### TMS Core API

`CoreAPIService` in `Integrations/ExternalApi/` is the template entry point for Core API integration.  
Configure `CoreApi:BaseUrl` and add new methods following `GetStatusAsync`.

### ACL V2 (Authorization)

ACL V2 behavior is implemented by:

- `ACLCheckingController` — gate page, auth-code exchange, verify, logout
- `AccessTokenValidationMiddleware` — per-request token check + refresh
- `AuthTokenRefreshService` — calls the refresh endpoint
- `UserAccessControlService` — loads roles + access controls into session
- All wired together in one place: `services.AddAuthAndAcl(...)` (see `Auth/AuthServiceExtensions.cs`)

If your ACL endpoints or payload contracts differ, update:

- `Auth` section values in `appsettings.json` (bound to `AuthOptions`)
- `ACLCheckingController.ExchangeAuthCodeAsync` parsing logic
- `UserAccessControlService.ParsePayload` if the roles/access envelope differs

### Page-Level Access Control

Per-route policy-style authorization based on the user's `accessControls` map.

How it flows:

1. After token verify, `ACLCheckingController.Verify` calls `Auth:UserRolesAndAccessUrl` (with bearer token) and stores the parsed `UserAclData` (user + roles + per-resource rights) **server-side** in `HttpContext.Session` under key `UserAclData`.
2. Controllers/actions are decorated with `[RequirePageAccess("<access name>", AccessRight.View|Add|Edit|Delete)]`. The access name must match a key in the `accessControls` dictionary.
3. `PageAccessAuthorizationFilter` runs before the action, reads the snapshot from session via `IUserAccessControlService`, and either lets the request through or short-circuits to `/Home/AccessDenied` (HTTP 403). AJAX/`/api/*` requests get a JSON 403 instead of a redirect.

Example (see `Controllers/Web/ProductManagementController.cs`):

```csharp
[Route("[controller]")]
[RequirePageAccess("PAB Sites", AccessRight.View)]
public class ProductManagementController : Controller
{
    [HttpGet("Create")]
    [RequirePageAccess("PAB Sites", AccessRight.Add)]
    public IActionResult Create() => View();

    [HttpGet("Edit/{id:int}")]
    [RequirePageAccess("PAB Sites", AccessRight.Edit)]
    public IActionResult Edit(int id) { ... }
}
```

Programmatic checks (e.g. inside an action or view):

```csharp
public class MyController : Controller
{
    private readonly IUserAccessControlService _acl;
    public MyController(IUserAccessControlService acl) { _acl = acl; }

    public IActionResult Index()
    {
        var canEdit = _acl.HasAccess(HttpContext, "PAB Sites", AccessRight.Edit);
        var snapshot = _acl.GetCurrent(HttpContext); // user, roles, full map
        return View(new { canEdit, snapshot });
    }
}
```

## Included Sample Endpoints

- `GET /index` health endpoint
- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products`
- `PUT /api/products/{id}`
- `DELETE /api/products/{id}`
- `GET /ProductManagement`

## Recommended Next Steps After Template Clone

1. Replace `ProductManagement` sample module with your real module.
2. Point UI loader URL to your TMS UI Core environment.
3. Set real ACL/Auth/Core API base URLs.
4. Add your branding assets (`logo`, title, etc.).
5. Move sample in-memory services to real data/API-backed services.

