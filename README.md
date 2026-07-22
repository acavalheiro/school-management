# ATL — School Management Platform

A multi-tenant web application for school management that serves as a communication channel between schools and student parents. Each school registers independently and gets fully isolated data.

## Features

- **Multi-tenant isolation** — every school's data is completely separate
- **Student management** — create and manage student records
- **Authentication** — JWT-based auth with role-based access control
- **Communication** — channel between school administrators and parents
- **REST API** — documented with Scalar (OpenAPI)
- **React frontend** — modern TypeScript UI

## Tech Stack

| Layer | Technology |
|---|---|
| Orchestration | .NET Aspire 13 (PostgreSQL in Docker, dashboard, telemetry) |
| API | .NET 10, ASP.NET Core Minimal APIs |
| Database | PostgreSQL + Entity Framework Core 10 |
| Auth | ASP.NET Core Identity + JWT Bearer |
| Validation | FluentValidation |
| API Docs | Scalar (OpenAPI) |
| Frontend | React + TypeScript |
| Testing | NUnit + AwesomeAssertions |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Aspire to run PostgreSQL)
- [Node.js](https://nodejs.org/) (for the frontend)

### 1. Configure secrets (first time only)

The JWT signing key and the seeded account passwords are **not** in source control. The API refuses to start without a valid signing key. Set them locally with user-secrets:

```bash
# A random 32+ byte signing key. Anyone holding it can mint a token for any
# school and any role, so never commit it or share it between environments.
dotnet user-secrets set "JwtSettings:Secret" "$(openssl rand -base64 48)" --project src/Api

# Only needed if you want the default accounts seeded in development.
dotnet user-secrets set "SuperAdminSettings:Password" "SuperAdmin123!" --project src/Api
dotnet user-secrets set "AdminSettings:Password" "Admin123!" --project src/Api
```

In deployed environments supply the same keys via environment variables (`JwtSettings__Secret`) or a key vault.

### 2. Run with .NET Aspire (recommended)

```bash
dotnet run --project src/AppHost
```

Aspire will:
- Pull and start **PostgreSQL** in Docker automatically
- Start **pgAdmin** (PostgreSQL web UI) on a random port
- Start the **API** and wait until Postgres is healthy
- Open the **Aspire dashboard** at `http://localhost:15888` with logs, traces, and resource status

The API URL, pgAdmin URL, and dashboard URL are all printed in the console on startup.

> The required Aspire environment variables (`ASPIRE_ALLOW_UNSECURED_TRANSPORT`, `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS`, OTLP endpoints, etc.) are pre-configured in `src/AppHost/Properties/launchSettings.json` — no manual setup needed.

On first run, migrations are applied automatically and the following are seeded:
- Roles: `SuperAdmin`, `Admin`, `User`
- Default super admin and admin users from `appsettings.json`

### 3. Run the frontend

```bash
cd src/ui
npm install
npm run dev
```

The frontend starts at `http://localhost:5173`.

---

### Alternative: Run without Aspire

If you prefer to manage PostgreSQL yourself:

```bash
# 1. Start PostgreSQL (e.g. via Docker)
docker run -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=atl -p 5432:5432 -d postgres

# 2. Apply migrations
dotnet ef database update -p src/Infrastructure -s src/Api

# 3. Run the API
dotnet run --project src/Api
```

## Default Credentials

| Role | Email | Password |
|---|---|---|
| Super Admin | superadmin@atl.com | from `SuperAdminSettings:Password` |
| Admin | admin@atl.com | from `AdminSettings:Password` |

Emails live in `appsettings.json`; passwords come from user-secrets or the environment. If no password is configured the account is simply not seeded, and a warning is logged. Seeding only runs in Development — never seed default accounts into a deployed environment.

## Multi-Tenancy

ATL uses a **shared database, shared schema** multi-tenancy model with three roles:

| Role | Scope | Can Do |
|---|---|---|
| **SuperAdmin** | All tenants | Create, rename, delete tenants |
| **Admin** | Own tenant | Manage students and users |
| **User** | Own tenant | View data |

- When a school **registers**, a new `Tenant` record is created with the provided school name — the registering user becomes the tenant's Admin
- All data is tagged with `TenantId` and filtered automatically via EF Core global query filters
- The `TenantId` travels as a `tid` claim inside the JWT — no extra headers needed

```
POST /api/auth/register   →  Creates Tenant (school name) + Admin user
POST /api/auth/login      →  Returns JWT with tid claim embedded
GET  /api/students        →  Returns only THIS school's students (automatic)
GET  /api/tenants         →  SuperAdmin only — lists all schools
```

## API Endpoints

### Authentication (anonymous)

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/register` | Register a new school (creates tenant) |
| POST | `/api/auth/login` | Login and receive JWT |

### Students (requires auth)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/students` | List all students in your school |
| GET | `/api/students/{id}` | Get a student by ID |
| POST | `/api/students` | Create a new student |

### User Management (Admin only)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/users` | List all users in your school |
| PUT | `/api/users/{id}/role` | Update a user's role |
| DELETE | `/api/users/{id}` | Delete a user |

### Tenant Management (Super Admin only)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/tenants` | List all tenants |
| GET | `/api/tenants/{id}` | Get a tenant by ID |
| POST | `/api/tenants` | Create a new tenant |
| PUT | `/api/tenants/{id}` | Rename a tenant |
| DELETE | `/api/tenants/{id}` | Delete tenant + all its data |

## Project Structure

```
src/
├── AppHost/                # .NET Aspire orchestration — PostgreSQL (Docker) + API
│   └── Properties/         # launchSettings.json with dashboard env vars
├── ServiceDefaults/        # Shared OpenTelemetry, health checks, service discovery
├── Api/                    # Minimal API endpoints, Program.cs, DI setup
├── Application/            # CQRS commands, queries, handlers, validators
│   ├── Common/             # Mediator, interfaces, Result<T>, AppRoles
│   ├── Students/           # Student commands & queries
│   ├── Auth/               # Register & login commands
│   ├── Tenants/            # Tenant CRUD commands & queries
│   └── Users/              # User management commands & queries
├── Domain/                 # Entities, value objects, enums, domain events
│   ├── Entities/           # Student, Tenant
│   ├── ValueObjects/       # Address
│   └── Common/             # Entity base, Result<T>, Error
├── Infrastructure/         # EF Core, Identity, JWT, tenant resolution
│   ├── Identity/           # ApplicationUser, TokenService, TenantService, IdentityService
│   └── Persistence/        # AppDbContext, EF configurations, migrations
└── ui/                     # React + TypeScript frontend

tests/
├── UnitTests/              # Domain logic and handler tests
└── IntegrationTests/       # Full API tests with WebApplicationFactory
```

## Development

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test
```

### Add a migration

```bash
dotnet ef migrations add <MigrationName> -p src/Infrastructure -s src/Api
```

### Format code

```bash
dotnet format
```

## Architecture

The application follows a clean layered architecture with CQRS:

```
API  →  Application (Commands/Queries)  →  Domain
                  ↓
           Infrastructure (EF Core, Identity, JWT)
```

**Key design decisions:**
- No repository pattern — handlers use `IAppDbContext` directly
- No AutoMapper — mappings are written explicitly
- No exceptions for business logic — `Result<T>` pattern throughout
- Tenant isolation is transparent to handlers — the DbContext returns pre-filtered data
