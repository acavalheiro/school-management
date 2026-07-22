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

**There is no pipeline/decorator support.** `Common/Behaviors/ValidationBehavior.cs` is written as a decorator with a `SetInner` method but is **never registered and never executes** — FluentValidation validators are registered in DI but nothing invokes them today. If you need validation to actually run, either wire the decorator into `AddApplication()` or validate explicitly in the handler; don't assume request validation is happening.

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

- **Validation does not run** (see the mediator section). `UpdateUserRoleCommandValidator` is the only thing restricting role assignment to `Admin`/`User`, so today an Admin can assign themselves `SuperAdmin` via `PUT /api/users/{id}/role` and reach the SuperAdmin-only tenant endpoints. Wiring the validation decorator is a security fix, not cleanup.
- `IdentityService.UpdateUserRoleAsync` removes existing roles before confirming the new one applied — a failed add leaves the user with no roles.
- `appsettings.json` is committed with a placeholder JWT secret and default credentials. Never rely on config-file secrets in a deployed environment; the app should fail startup on a placeholder or sub-32-byte secret.
- Login goes through `UserManager.CheckPasswordAsync`, not `SignInManager`, so **Identity lockout never triggers**. There is no rate limiting on `/api/auth/login`.
- JWTs cannot be revoked. Deleting or demoting a user leaves their token valid until expiry.
- `/api/auth/register` returns raw Identity errors, which enumerates existing emails.

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
- Integration tests use `WebAppFactory` (`WebApplicationFactory<Program>`), which swaps PostgreSQL for EF Core InMemory, replaces `ITenantService` with a fixed `TestTenantId`, and installs `TestAuthHandler` so every request is auto-authenticated as an `Admin`. Note it removes `IDbContextOptionsConfiguration<AppDbContext>` registrations too — EF Core 10 accumulates them per `AddDbContext` call and both providers would otherwise apply.
- `Program.cs` ends with `public partial class Program;` so the factory can reference it.
- Test naming: `[Method]_[Scenario]_[ExpectedResult]`.
- **Gap:** the factory pins a single `TestTenantId` and auto-authenticates every request as `Admin`, so no existing test can catch a cross-tenant leak. Any change to the query filter, `TenantService`, or file access needs a test with two distinct tenants and real tokens.

## Git Workflow

Branches: `feature/`, `bugfix/`, `hotfix/`. Commits: `type: description` (feat, fix, refactor, test, docs). Branch before making changes; run tests before committing.
