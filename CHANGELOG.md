# Changelog

All notable changes to the **TMS Template (.NET 8)** project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## Current Version

| | |
|---|---|
| **Latest release** | `v0.2.1-alpha` (June 10, 2026) |
| **Running on `main`** | `v0.2.1-alpha` |
| **Target framework** | .NET 8.0 |

---

## [v0.2.1-alpha] — 2026-06-10

[Compare changes](https://10.200.1.66/Development/tms-template-net8/compare/v0.2.0-alpha...v0.2.1-alpha)

### Added

- Dynamic sidebar rendering — sidebar items are now loaded from the ACL API at runtime instead of being hardcoded

### Fixed

- Replaced custom footer markup with the UI Foundation `<tms-footer>` component in the shared layout

---

## [v0.2.0-alpha] — 2026-06-08

[Compare changes](https://10.200.1.66/Development/tms-template-net8/compare/v0.1.0-alpha...v0.2.0-alpha)

### Added

- **dotnet CLI module template** — scaffold new CRUD modules quickly via `dotnet new tms-module`
- **Controller → Service → Repository architecture** — standardized layering across the template
- Template generator source files for module scaffolding
- `Logs/` added to `.gitignore`

### Changed

- Consolidated all authentication-related code into a single `Auth/` folder
- Refactored project structure to align with the new layered architecture

### Fixed

- Fixed auth token exchange URL (`Auth:BaseUrl`)
- Fixed error on Product Management index page
- Fixed blank Dashboard page (now displays the Home index view)

### Documentation

- Updated README with module template generator usage instructions

---

## [v0.1.0-alpha] — 2026-04-29

Initial alpha release of the TMS .NET 8 web application template.

[Full changelog](https://10.200.1.66/Development/tms-template-net8/commits/v0.1.0-alpha)

### Added

- **Base .NET 8 framework** — ASP.NET Core MVC project scaffold with Web and API controller separation
- **UI Core layout** — integrated TMS UI Foundation layout, topbar, and sidebar via `TMS.WebApp.Sdk`
- **ACL integration** — Central Auth ACL checking for in-app page access and route-based access control
- **Sample CRUD module** — Product Management module as a reference implementation (views, controllers, API endpoints)
- **Core API service** — HTTP client service for internal API communication
- **RSA public key loading** — dynamically loads the public key from the ACL API on application startup
- **Database connection setup** — SQL Server connection configuration and repository pattern foundation
- **External integrations folder** — consolidated external API and database integrations under `Integrations/`
- **File logger extension** — structured file-based logging support
- **Font Awesome** — local static asset for iconography
- **`appsettings.Development.json`** — development environment configuration template
- **TMS.SDK** — installed and wired for TMS platform services
- **Common modules** — ported shared modules from the legacy TMS standard template
- **README** — project setup and usage guide for bootstrapping new applications from this template
- **API index page** — health/info endpoint for the API surface

### Changed

- Refactored extension methods for app-specific functions and authentication
- Refactored external service integrations into the `Integrations/` folder structure
- Simplified codebase — removed unnecessary abstractions and dead code across multiple passes

### Fixed

- ACL checking API endpoint URL corrections
- ACL redirect now points to DSP base URL instead of `null`
- Logging attribute arrangement fix
- UI styling corrections in common modules
- Async task `await` usage in module controllers
- Dynamic `public.pem` loading (replaces pre-filled key file in the project)

---

## Release Tags

| Tag | Date | Commit |
|-----|------|--------|
| `v0.2.1-alpha` | 2026-06-10 | `6d6502b` |
| `v0.2.0-alpha` | 2026-06-08 | `da70915` |
| `v0.1.0-alpha` | 2026-04-29 | `4b2c833` |
