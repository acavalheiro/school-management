# Plan: User Language & Site Translations — V1

Implements `spec/SPEC-i18n.md` on branch `feature/user-language-translations`.

## Context
ATL serves UK and Portuguese schools, but every UI string is hard-coded English and dates follow the
browser's locale. V1 adds a per-user saved language (`en-GB` / `pt-PT`) and translates the whole React UI
with i18next + react-i18next. Spec decisions: the language is stored as a string code, not an FK; server
error messages stay English and the frontend maps them by status; the repo owner reviews pt-PT copy;
Tenant → Escola, Student → Aluno; the switcher sits in the sidebar and on the login page.

**Change from the spec agreed during planning:** the UI has no register page (`App.tsx` routes only
`/login`), so client-side translated validation covers **create-student only**. Register-form validation
moves to Deferred.

## Step 0 — housekeeping (done)
- Spec: `Status: approved`; drop the register-form rules from "Client-side form validation" and success
  criterion 11; add "register page + its validation" to Out of Scope.
- Write this plan to `tasks/plan.md` and the checklist below to `tasks/todo.md` (neither exists yet).

## Findings that shape the plan
- **No "current user id" abstraction exists.** Real JWTs carry `sub` (`MapInboundClaims = false` in
  `src/Api/Program.cs:33`; `SecurityStampValidator` already reads `JwtRegisteredClaimNames.Sub`).
  → Add `ICurrentUserService { Guid UserId }` in Application, implemented in Infrastructure by reading `sub`,
  in the same style as `TenantService` (`src/Infrastructure/Identity/TenantService.cs`).
- **`TestAuthHandler` emits a random `NameIdentifier`, not `sub`** (`tests/IntegrationTests/Common/WebAppFactory.cs:149`).
  → Add `sub` from a new `X-Test-User` header (default: random GUID), plus `CreateClientFor(tenantId, role, userId)`
  and a `SeedUserAsync(email, tenantId, role)` helper that uses `UserManager`. Without these, `/api/me` can't be tested.
- Migrations live in `src/Infrastructure/Migrations` (not `Persistence/Migrations`, which is empty).
- `ApplicationUser` has no EF configuration class; configure `PreferredLanguage` in `AppDbContext.OnModelCreating`
  (`HasMaxLength(10).IsRequired().HasDefaultValue("en-GB")`).
- Endpoints return `Results.Problem(description, statusCode)`: the error **code** is not on the wire, so V1
  error mapping is by HTTP status.
- UI `request` helpers throw `Error("<status>: <body>")` (`src/ui/src/api/authApi.ts`, `studentsApi.ts`,
  `tenantsApi.ts`), and pages display `e.message` directly.
- The frontend has Vitest + RTL with a careful `src/test/setup.ts` (localStorage shim, matchMedia, etc.).
  i18n init goes there. CLAUDE.md's "no frontend test setup" line is stale.
- `date-fns` is already installed transitively (via react-day-picker); use `enGB` / `pt` locales for the
  calendar. Date call sites: `StudentsPage.tsx:245,338`, `TenantsPage.tsx:179`, `components/ui/calendar.tsx:33,155`.

## Dependency graph
```
SupportedLanguages + PreferredLanguage column ──► GET /api/me ──► PUT /api/me/language
                                                        │                │
i18n core + LanguageProvider ──► Layout/switcher ──► Login page ──► server sync (meApi)
                                         │
                              ApiError + error mapping ──► Students ──► Tenants ──► ui/* sweep
```
The backend (T1–T3) and the frontend foundation (T4–T5) are independent. T6 (server sync) needs both.

## Tasks (vertical slices)

### Phase A — Backend
- [ ] **T1. Language column.** `Application/Common/SupportedLanguages.cs` (`EnGb`, `PtPt`, `All`, `Default`);
  `ApplicationUser.PreferredLanguage = SupportedLanguages.Default`; EF config; migration `AddUserPreferredLanguage`.
  - Accept: the migration adds a non-null `varchar(10)` with default `'en-GB'`; `dotnet build` passes.
  - Verify: inspect the migration; `dotnet ef migrations add AddUserPreferredLanguage -p src/Infrastructure -s src/Api`.
  - Files: SupportedLanguages.cs, ApplicationUser.cs, AppDbContext.cs, Migrations/*.
- [ ] **T2. `GET /api/me`.** `ICurrentUserService` + `CurrentUserService` (reads `sub`, `Guid.Empty` if missing)
  registered in `DependencyInjection.cs`; `GetCurrentUserQuery` → `IIdentityService.GetCurrentUserAsync(userId)`
  → `CurrentUserDto(Id, Email, Role, PreferredLanguage)`; `Api/Endpoints/MeEndpoints.cs` (`.RequireAuthorization()`),
  mapped in `Program.cs`. Test infra: `sub` claim, `X-Test-User`, `SeedUserAsync`.
  - Accept: a new user gets `en-GB`; anonymous → 401; `Guid.Empty`/unknown user → 404.
  - Verify: `dotnet test --filter "FullyQualifiedName~Me"`.
- [ ] **T3. `PUT /api/me/language`.** `UpdateMyLanguageCommand` (Language only, no user id) + validator (exact
  match against `SupportedLanguages.All`) + handler (uses `ICurrentUserService.UserId`) →
  `IIdentityService.UpdatePreferredLanguageAsync`, which re-checks the whitelist and does **not** touch the security stamp.
  `UpdateMyLanguageRequest` record at the bottom of `MeEndpoints.cs`.
  - Accept: `pt-PT` → 204 and GET returns `pt-PT`; `pt-BR`/`en`/`""`/`PT-pt` → 400; works for User, Admin
    and SuperAdmin; seed users A and B, A's PUT leaves B unchanged; the token is still valid after the change.
  - Verify: unit tests (validator, handler) + integration tests + a case in `ValidationPipelineTests`; `dotnet test`.
- **Checkpoint A:** `dotnet test` green. Manually against Aspire with a real JWT: `GET`/`PUT /api/me`
  (this proves `sub` resolution, which TestAuthHandler cannot).

### Phase B — Frontend foundation
- [ ] **T4. i18n core + sidebar.** `npm i i18next react-i18next`; `src/i18n/{index.ts, i18next.d.ts, languages.ts,
  locales/en-GB.ts, locales/pt-PT.ts, LanguageProvider.tsx}`; `components/LanguageSwitcher.tsx`; wire into
  `main.tsx`/`App.tsx`; translate `Layout.tsx` (nav, theme toggle, sign out); init `en-GB` in `test/setup.ts`.
  - Accept: the switcher changes sidebar text and `<html lang>` without a reload; the choice persists in `atl_lang`;
    removing a pt-PT key or using an unknown key fails `npm run build`.
  - Verify: `npm test`, `npm run build`, `npm run lint`; new `LanguageProvider.test.tsx`, `Layout.test.tsx` cases.
- [ ] **T5. Login page + detection.** Translate `LoginPage.tsx`, add the switcher; precedence is
  localStorage > `navigator.languages` (`pt*` → pt-PT) > en-GB.
  - Accept: precedence tests pass; the login page renders in pt-PT for a `pt-BR` browser.
- [ ] **T6. Server sync.** `api/meApi.ts` (`get`, `updateLanguage`); after login and on app load with a token,
  `GET /api/me` and adopt its language; `setLanguage` PUTs only when signed in (optimistic, non-blocking
  translated error on failure).
  - Accept: tests with a mocked fetch cover "server beats localStorage", "no PUT when anonymous", and
    "PUT failure keeps the local choice".
- **Checkpoint B:** all frontend checks green. Manually: switch language, reload, sign in on a fresh browser
  profile, and confirm the saved language is applied.

### Phase C — Translate the app
- [ ] **T7. Error mapping.** Shared `api/http.ts` with `ApiError(status, body)`, used by all three api modules;
  `i18n/errors.ts` → `errorMessage(err, t)` maps 400/401/403/404/409/429/5xx/network to `errors.*` keys.
  Replace every `setError(e.message)`.
  - Accept: no page renders a raw `"<status>: …"`; unit tests for the mapping.
- [ ] **T8. Dashboard + Students.** Translate both pages; `formatDate` from `useLanguage()` replaces
  `toLocaleDateString(undefined…)`; calendar gets a date-fns locale; translated client validation in the
  create-student dialog (names, email, valid email, DOB in the past) blocks submit.
  - Accept: existing Students tests pass; new tests cover each validation message in both languages.
- [ ] **T9. Tenants + TenantDetail.** Translate (Escola terminology in pt-PT), dates, dialogs, confirmations.
- [ ] **T10. Primitives + sweep.** Translate `components/ui/*` labels (calendar aria, dialog close, combobox
  placeholders); grep JSX in `pages/` and `components/` for leftover literals; add a test that no pt-PT value
  is empty.
- **Checkpoint C:** full manual walkthrough of every page in both languages (long-string truncation, date
  picker). **Hand off pt-PT copy for your review**, then apply your corrections.

### Phase D — Wrap-up
- [ ] **T11. Docs.** CLAUDE.md: `/api/me`, `ICurrentUserService`, i18n conventions (add en-GB first; pt-PT
  vocabulary), fix the stale "no frontend test setup" line, and note that `X-Test-User` exists. Fold the
  outcome into the spec.

## Verification (end-to-end)
- `dotnet build && dotnet test`; in `src/ui`: `npm run build && npm run lint && npm test`.
- `dotnet run --project src/AppHost` + `npm run dev`: register/login via the API, switch to pt-PT in the
  sidebar, reload, log in from a private window, and confirm pt-PT applies. Check dates and the date picker
  on Students, and that an API error (e.g. a duplicate) shows a translated message.
- Security: integration test A≠B, and a real JWT confirms `/api/me` acts only on the token's `sub`. The
  role-change token revocation still works (language change must not revoke).

## Risks
- **Test infra change to `TestAuthHandler`:** adding `sub` must not alter tenant/role behaviour, so the
  existing suites guard it.
- **Existing frontend tests assert English text:** mitigated by initialising `en-GB` in setup.
- **Long pt-PT strings in the 14rem sidebar:** checked at Checkpoint C.
