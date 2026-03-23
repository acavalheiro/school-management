# CLAUDE.md - [ATL]

## Overview
School Management Web app to get student information (Photos, Invoices, Communication) that will be used as a communication channel between school and student parents. The application is **multi-tenant**: each admin user owns their own isolated school/tenant.

## Tech Stack
- .NET 10, ASP.NET Core Minimal APIs
- Entity Framework Core 10 with PostgreSQL
- ASP.NET Core Identity with EF Core for authentication
- JWT Bearer tokens for API authorization
- Own Implementation of CQRS
- FluentValidation for request validation
- Scalar for OpenAPI documentation
- NUnit + AwesomeAssertions for testing
- React + Typescript for the Frontend

## Project Structure
- `src/AppHost/` - .NET Aspire orchestration (starts PostgreSQL in Docker + API)
- `src/ServiceDefaults/` - Shared OpenTelemetry, health checks, service discovery config
- `src/Api/` - Endpoints, middleware, DI configuration
- `src/Application/` - Commands, queries, handlers, validators
- `src/Domain/` - Entities, value objects, enums, domain events
- `src/Infrastructure/` - EF Core, Identity, JWT, external services
- `src/Infrastructure/Identity/` - ApplicationUser, IdentityService, TokenService, TenantService, JwtSettings
- `tests/UnitTests/` - Domain and application layer tests
- `tests/IntegrationTests/` - API and database tests
- `src/ui` - Frontend, Web Application

## Commands
- Build: `dotnet build`
- Test: `dotnet test`
- **Run with Aspire (recommended):** `dotnet run --project src/AppHost` — starts PostgreSQL in Docker + API + Aspire dashboard at `http://localhost:15888`
  - Required Aspire env vars are pre-set in `src/AppHost/Properties/launchSettings.json` (dashboard URLs, OTLP endpoints, `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true`)
- Run API standalone: `dotnet run --project src/Api` (requires PostgreSQL already running)
- Add Migration: `dotnet ef migrations add <Name> -p src/Infrastructure -s src/Api`
- Update Database: `dotnet ef database update -p src/Infrastructure -s src/Api`
- Format: `dotnet format`

## Architecture Rules
- Domain layer has ZERO external dependencies
- Application layer defines interfaces, Infrastructure implements them
- All database access goes through EF Core DbContext (no repository pattern)
- Use Mediator for all command/query handling
- API layer is thin - endpoint definitions only
- `ApplicationUser` (Identity) lives in Infrastructure — Domain stays clean
- `IIdentityService` defined in Application, implemented in Infrastructure
- JWT config lives in `appsettings.json` under `JwtSettings` section
- Three roles: `SuperAdmin`, `Admin`, `User` (constants in `Application.Common.AppRoles`)
- Roles and default users seeded on startup via `RoleSeeder` (runs after migrations in Dev)
- Default admin credentials configured under `AdminSettings` in `appsettings.json`
- Default super admin credentials configured under `SuperAdminSettings` in `appsettings.json`

## Multi-Tenancy

### Model
- **Shared database, shared schema** with EF Core global query filters
- `Tenant` is an explicit Domain entity with `Id` (Guid) and `Name` (string)
- When a user registers, a new `Tenant` record is created alongside the user account
- `ApplicationUser.TenantId` references `Tenant.Id`; all domain entities carry the same `TenantId`
- `TenantId` is embedded in the JWT as the `tid` claim and automatically applied as a query filter

### Roles and Tenant Access
| Role | Tenant | Data Access |
|---|---|---|
| `SuperAdmin` | None (`Guid.Empty`) | All tenants — manages tenant lifecycle |
| `Admin` | Own tenant | Own tenant's data only |
| `User` | Own tenant | Own tenant's data only |

### Key Components
- `Tenant` entity (Domain) — `Id`, `Name`, `CreatedAt`; `Rename()` method
- `ITenantService` (Application) — interface exposing `TenantId` for the current request
- `TenantService` (Infrastructure) — reads the `tid` claim from `IHttpContextAccessor`
- `AppDbContext.TenantId` — settable property applied by the `IAppDbContext` DI factory
- `IAppDbContext` is registered as a **scoped factory** that resolves `AppDbContext` and sets `TenantId` from `ITenantService` before returning it to callers

### Isolation Rules
- Handlers resolve `IAppDbContext` — they always get the tenant-filtered view automatically
- Identity tables (`AspNetUsers`, etc.) are NOT filtered at the DbContext level; `IdentityService` filters users explicitly via `.Where(u => u.TenantId == tenantId)`
- `UpdateUserRoleAsync` and `DeleteUserAsync` verify the target user belongs to the caller's tenant (cross-tenant protection)
- `TenantId == Guid.Empty` bypasses the query filter — used by SuperAdmin, seeder, and migrations
- `Tenant` records have no query filter — they are always globally visible (SuperAdmin only endpoint)

### SuperAdmin Tenant Management
- `POST /api/tenants` — Create a standalone tenant (no user, SuperAdmin only)
- `PUT /api/tenants/{id}` — Rename a tenant
- `DELETE /api/tenants/{id}` — Delete tenant + all its students + all its users
- `GET /api/tenants` / `GET /api/tenants/{id}` — List / get tenants

### Adding a New Tenant-Scoped Entity
1. Add `public Guid TenantId { get; private set; }` to the Domain entity
2. Pass `tenantId` through the factory method
3. Map the column and add an index in the EF Core `IEntityTypeConfiguration`
4. Extend the global query filter in `AppDbContext.OnModelCreating` for the new entity
5. In the command handler, inject `ITenantService` and pass `tenantService.TenantId` to the factory

## Code Conventions

### Naming
- Commands: `Create[Entity]Command`, `Update[Entity]Command`
- Queries: `Get[Entity]Query`, `List[Entities]Query`
- Handlers: `[Command/Query]Handler`
- DTOs: `[Entity]Dto`, `Create[Entity]Request`

### Patterns We Use
- Primary constructors for DI
- Records for DTOs and commands
- Result<T> pattern for error handling (no exceptions for flow control)
- File-scoped namespaces
- Always pass CancellationToken to async methods
- Auth endpoints are anonymous; all other endpoints require `.RequireAuthorization()`
- User management endpoints (`/api/users`) require `.RequireAuthorization(AppRoles.Admin)`
- Frontend stores JWT in `localStorage` via `tokenStore` helper and sends it as `Authorization: Bearer <token>`
- JWT contains role claims (`ClaimTypes.Role`) and tenant claim (`tid`) so `RequireRole` and tenant resolution work out of the box

### Patterns We DON'T Use (Never Suggest)
- Repository pattern (use EF Core directly)
- AutoMapper (write explicit mappings)
- Exceptions for business logic errors
- Stored procedures

## Validation
- All request validation in FluentValidation validators
- Validators auto-registered via assembly scanning
- Validation runs in Mediator pipeline behavior

## Testing
- Unit tests: Domain logic and handlers
- Integration tests: Full API endpoint testing with WebApplicationFactory
- Use AwesomeAssertions for readable assertions
- Test naming: `[Method]_[Scenario]_[ExpectedResult]`
- `WebAppFactory` replaces the real DB with EF Core InMemory, stubs `ITenantService` with a fixed `TestTenantId`, and uses `TestAuthHandler` to auto-authenticate all requests

## Git Workflow
- Branch naming: `feature/`, `bugfix/`, `hotfix/`
- Commit format: `type: description` (feat, fix, refactor, test, docs)
- Always create a branch before changes
- Run tests before committing

## Domain Terms
- **Tenant** — A school; maps to `TenantId` (Guid) on `ApplicationUser` and all domain entities
- **Admin** — The school administrator; owns the tenant and all its data
- **Student** — A student enrolled in the school; scoped to a tenant
