# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

ATL — a multi-tenant school management web app used as a communication channel between schools and student parents (photos, invoices, messages). Each registering admin owns an isolated school/tenant.

## Tech Stack

.NET 10 / ASP.NET Core Minimal APIs · EF Core 10 + PostgreSQL · ASP.NET Core Identity + JWT Bearer · hand-rolled CQRS mediator · FluentValidation · Scalar (OpenAPI) · .NET Aspire orchestration · NUnit + AwesomeAssertions + Moq · React 19 + TypeScript + Vite + Tailwind v4 + shadcn-style Radix components.

Solution file is `Atl.slnx` (the newer XML solution format), not a `.sln`.

## Commands

### Backend

```bash
dotnet build
dotnet test

# Run a single test project / class / test
dotnet test tests/UnitTests
dotnet test --filter "FullyQualifiedName~StudentTests"
dotnet test --filter "Name=Create_WithValidData_ReturnsSuccess"

dotnet format

# EF Core — Infrastructure holds the migrations, Api is the startup project
dotnet ef migrations add <Name> -p src/Infrastructure -s src/Api
dotnet ef database update -p src/Infrastructure -s src/Api
```

### Running the app

```bash
# Recommended — Aspire starts PostgreSQL + pgAdmin in Docker, then the API
dotnet run --project src/AppHost      # dashboard at http://localhost:15888

# Standalone (requires PostgreSQL already listening on 5432)
dotnet run --project src/Api
```

Aspire env vars (`ASPIRE_ALLOW_UNSECURED_TRANSPORT`, OTLP endpoints, dashboard URLs) are pre-set in `src/AppHost/Properties/launchSettings.json`. In Development only, `Program.cs` applies migrations and runs `RoleSeeder` on startup.

### Frontend (`src/ui`)

```bash
npm install
npm run dev      # http://localhost:5173 — the only origin allowed by the API's CORS policy
npm run build    # tsc -b && vite build
npm run lint     # eslint
```

There is no frontend test setup. API base URL comes from `VITE_API_URL`, defaulting to `https://localhost:7000`. `@/` aliases `src/ui/src/`.

## Architecture

```
Api (endpoints) → Application (commands/queries/handlers) → Domain
                            ↑
                  Infrastructure implements Application interfaces
```

- **Domain** has zero external dependencies. `Entity`, `Result<T>`/`Result`, `Error` live in `Domain/Common`.
- **Application** defines `IAppDbContext`, `IIdentityService`, `ITenantService`; Infrastructure implements them.
- **Api** is thin: each `*Endpoints.cs` maps a route group, builds a command/query, sends it through `IMediator`, and translates `Result` into `Results.Ok/NotFound/Problem`.

### The custom mediator

`Application/Common/Mediator` is a hand-written ~20-line mediator, not MediatR. `Mediator.Send` resolves `IRequestHandler<TRequest, TResponse>` from the container by reflection and invokes `Handle`. `AddApplication()` scans the Application assembly and registers every closed `IRequestHandler<,>` implementation.

`Common/Behaviors/ValidationBehavior.cs` is a decorator (not a true pipeline): `AddApplication()` registers each concrete handler by its own type, then registers `IRequestHandler<,>` as a factory that wraps it in `ValidationBehavior` via `SetInner`. Resolving a handler therefore always yields the validation decorator, so FluentValidation validators run before every handler. Only one decorator is supported — adding a second behavior means nesting it in that same factory.

### Result pattern

No exceptions for business-logic errors. Handlers return `Result<T>`/`Result`; `Error` implicitly converts to a failure and `T` to a success, so `return result.Error;` and `return student.Id;` both work. Endpoints branch on `IsSuccess`.

## Multi-Tenancy

Shared database, shared schema, enforced by EF Core global query filters.

- `Tenant` is a Domain entity (`Id`, `Name`, `CreatedAt`, `Rename()`). Registering a user creates a `Tenant` alongside the account.
- `TenantId` lives on `ApplicationUser` and every tenant-scoped domain entity, and travels in the JWT as the `tid` claim.
- `TenantService` (Infrastructure) reads `tid` off `IHttpContextAccessor`.
- `IAppDbContext` is registered as a **scoped factory** that resolves `AppDbContext` and stamps `db.TenantId` from `ITenantService` before handing it out. Handlers that take `IAppDbContext` therefore always see a pre-filtered view; a handler taking `AppDbContext` directly does not.
- The filter is `TenantId == Guid.Empty || e.TenantId == TenantId` — `Guid.Empty` **bypasses** it, which is how SuperAdmin, the seeder, and migrations see everything.
- `Tenant` itself has no query filter; it is globally visible (SuperAdmin endpoints only).
- Identity tables are not filtered at the DbContext level. `IdentityService` filters users explicitly, and `UpdateUserRoleAsync` / `DeleteUserAsync` verify the target user belongs to the caller's tenant.

### Roles

`SuperAdmin` (tenant `Guid.Empty`, all data, manages tenant lifecycle), `Admin` (own tenant, manages students/users), `User` (own tenant, read). Constants in `Application.Common.AppRoles`; policies of the same names are added in `Program.cs`. Seeded on startup from `SuperAdminSettings` / `AdminSettings` in `appsettings.json`.

### Adding a tenant-scoped entity

1. Add `public Guid TenantId { get; private set; }` to the Domain entity and take it in the factory method.
2. Map the column + index in an `IEntityTypeConfiguration`.
3. Add the entity to the global query filter in `AppDbContext.OnModelCreating`.
4. In the handler, inject `ITenantService` and pass `tenantService.TenantId` to the factory.

## Security

This app stores personal data about **minors** (names, dates of birth, addresses, photos, invoices). Treat tenant isolation and file access as correctness-critical, not best-effort. When a change touches auth, the tenant filter, or file serving, say so explicitly and add a test.

### Known gaps — do not assume these are handled

- `appsettings.json` is committed with a placeholder JWT secret and default credentials. Never rely on config-file secrets in a deployed environment; the app should fail startup on a placeholder or sub-32-byte secret.
- Login goes through `UserManager.CheckPasswordAsync`, not `SignInManager`, so **Identity lockout never triggers**. There is no rate limiting on `/api/auth/login`.
- JWTs cannot be revoked. Deleting or demoting a user leaves their token valid until expiry.
- `/api/auth/register` returns raw Identity errors, which enumerates existing emails.
- `RegisterCommandHandler` saves the `Tenant` before creating the user, with no transaction — a failed registration orphans a `Tenant` row, and the endpoint is anonymous.
- `dotnet build` reports known vulnerabilities in transitive packages (`Microsoft.OpenApi`, `MessagePack` via AppHost, OpenTelemetry). Worth resolving before handling real student data.

### Role assignment

`AppRoles.Assignable` (`Admin`, `User`) is the whitelist for `/api/users/{id}/role`; `SuperAdmin` is deliberately excluded because it grants cross-tenant access. It is enforced twice on purpose — in `UpdateUserRoleCommandValidator` and again in `IdentityService.UpdateUserRoleAsync` — so the boundary survives a DI or pipeline regression. Keep both. `UpdateUserRoleAsync` also adds the new role before removing old ones, so a failed add cannot strip a user of every role.

### Tenant isolation

- The filter is **fail-open**: an unresolvable `tid` claim yields `Guid.Empty`, which *disables* the filter rather than denying the request. Prefer an explicit bypass flag for SuperAdmin over the `Guid.Empty` sentinel, and never widen the set of code paths that run with `Guid.Empty`.
- Never call `IgnoreQueryFilters()` outside a SuperAdmin-only handler.
- Injecting `AppDbContext` directly instead of `IAppDbContext` silently skips the tenant stamp. Always take `IAppDbContext`.
- Identity tables are unfiltered — any new user-facing query must filter on `TenantId` explicitly and verify the target user's tenant matches the caller's.
- Frontend `RequireRole` is cosmetic. Authorization is only real when enforced server-side.

### Handling student files (photos, invoices)

Not yet implemented. When it is:

- No public or guessable URLs, and no static-file serving of user content. Every download goes through an authorized endpoint that loads the owning record and re-checks its `TenantId` against the caller's.
- Store blobs outside the database with private ACLs. Issue short-lived pre-signed URLs per request; never persistent ones.
- Include the tenant in the object key (`{tenantId}/{studentId}/{fileId}`) and validate the key's tenant prefix before streaming, so an app-layer bug cannot cross tenants silently.
- On upload: verify type by magic bytes (not `Content-Type` or extension), whitelist formats, cap size, generate a server-side filename, and **strip EXIF from images** — geolocation in photos of children is a real risk.
- On download: `Content-Disposition: attachment` and `X-Content-Type-Options: nosniff`.
- Encrypt at rest, audit-log every access (who, when, which student), and make deletion remove the blob, not just the row. Photos of minors need a recorded consent basis and a working right-to-erasure path.

## Conventions

- Naming: `Create[Entity]Command`, `Get[Entity]Query` / `List[Entities]Query`, `[Command|Query]Handler`, `[Entity]Dto`, `Create[Entity]Request`.
- Primary constructors for DI; records for DTOs/commands; file-scoped namespaces; `CancellationToken` on every async method.
- Auth endpoints are anonymous; everything else uses `.RequireAuthorization()`. `/api/users` requires `.RequireAuthorization(AppRoles.Admin)`; `/api/tenants` requires SuperAdmin.
- Request DTO records are declared at the bottom of the endpoint file they belong to.

### Never suggest

Repository pattern (use `IAppDbContext`/EF Core directly), AutoMapper (mappings are explicit), exceptions for business-logic flow, stored procedures.

## Frontend notes

- JWT is stored in `localStorage` under `atl_token` via `tokenStore` in `src/api/authApi.ts`; `AuthContext` decodes it client-side (base64 payload) to read email, role, and `tid`.
- The role claim key is the full `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` URI.
- Routing lives in `App.tsx` with `RequireAuth` / `RequireRole` wrappers around a shared `Layout`.
- `components/ui/*` are shadcn-style Radix primitives; compose with `cn()` from `lib/utils.ts`.

## Testing

- Unit tests cover Domain entities and Application handlers, using Moq and `tests/UnitTests/Common/MockDbSet.cs` to fake `DbSet`s.
- Integration tests use `WebAppFactory` (`WebApplicationFactory<Program>`), which swaps PostgreSQL for EF Core InMemory and installs `TestAuthHandler` so every request is auto-authenticated. Note it removes `IDbContextOptionsConfiguration<AppDbContext>` registrations too — EF Core 10 accumulates them per `AddDbContext` call and both providers would otherwise apply. The in-memory database name is a factory field, not generated inside the options lambda; generating it there gives each scope its own database and silently hides seeded data.
- `ITenantService` is **not** stubbed to a fixed value. `ClaimsTestTenantService` reads the `tid` claim exactly as production does, and `TestAuthHandler` takes the tenant and role from the `X-Test-Tenant` / `X-Test-Role` headers (defaulting to `TestTenantId` / `Admin`). Use `factory.CreateClientFor(tenantId, role)` to act as a given tenant and `factory.SeedAsync(...)` to arrange data for other tenants with the filter bypassed.
- `TenantIsolationTests` covers the cross-tenant read invariant. When changing the query filter or tenant resolution, confirm those tests **fail** if you deliberately break isolation — an isolation test that passes vacuously on an empty result set is worse than none.
- `Program.cs` ends with `public partial class Program;` so the factory can reference it.
- `ValidationPipelineTests` guards the DI wiring that makes validators run at all. Don't delete it — the rules it protects are security boundaries, and they failed open once already.
- Test naming: `[Method]_[Scenario]_[ExpectedResult]`.

## Git Workflow

Branches: `feature/`, `bugfix/`, `hotfix/`. Commits: `type: description` (feat, fix, refactor, test, docs). Branch before making changes; run tests before committing.
