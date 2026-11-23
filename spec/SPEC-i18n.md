# Spec: User Language & Site Translations — V1

Status: approved (2026-11-23). Plan: `tasks/plan.md`

## Objective

ATL is used by schools in the UK and Portugal. Today every UI string is hard-coded English and dates
render in whatever locale the browser happens to report. V1 lets each user choose their language —
**English (UK, `en-GB`)** or **Portuguese (Portugal, `pt-PT`)** — and renders the whole React UI in it.

**Users:** every role (SuperAdmin, Admin, User), plus anonymous visitors on the login page.

**User stories**

1. As a visitor on the login page, I see the site in my browser's language if it is supported
   (any `pt*` → `pt-PT`, otherwise `en-GB`), and I can switch language before signing in.
2. As a signed-in user, I can change my language from the sidebar; the UI switches immediately,
   without a reload or re-login.
3. As a signed-in user, my choice is saved to my account, so it follows me to another browser or device.
4. Dates are formatted for the active language (e.g. `23/11/2026` / `23 Nov 2026` vs.
   `23/11/2026` / `23 de nov. de 2026`), including the date-picker month/weekday names.

**Language precedence** (highest first):
1. Saved account preference (once signed in)
2. Locally cached choice (`localStorage` key `atl_lang`)
3. Browser language match (`navigator.languages`)
4. `en-GB` (default and fallback)

## Design Summary

### Backend
- `ApplicationUser.PreferredLanguage` — `string`, max 10, non-null, default `'en-GB'`. **Schema change + EF
  migration** (`AddUserPreferredLanguage`); existing rows backfilled to `en-GB` by the column default.
- `Application.Common.SupportedLanguages` — `EnGb = "en-GB"`, `PtPt = "pt-PT"`, `All`, `Default`.
  Single whitelist, mirrored by the frontend's language list.
- New route group `/api/me` — `.RequireAuthorization()` (any role, **not** Admin-only like `/api/users`):
  - `GET  /api/me` → `CurrentUserDto(Guid Id, string Email, string Role, string PreferredLanguage)`
  - `PUT  /api/me/language` body `UpdateMyLanguageRequest(string Language)` → `204`
- `UpdateMyLanguageCommand` + validator (whitelist check) + handler → `IIdentityService.UpdatePreferredLanguageAsync(userId, language, ct)`,
  which **also** re-checks the whitelist (same double-enforcement pattern as role assignment).
- The target user id comes **only** from the caller's `sub` claim — never from the route or body. Identity
  tables are unfiltered, so this is the isolation boundary for this endpoint.
- Language is **not** put in the JWT: a changed preference would be stale until the next login.
- Changing language does **not** call `UpdateSecurityStampAsync` (it is not a security-relevant change and
  must not log the user out).

### Frontend
- Translation catalogues in `src/ui/src/i18n/locales/en-GB.ts` and `pt-PT.ts`, one `translation` namespace.
  `en-GB` is the source of truth; `pt-PT` is typed against it, so **a missing or extra key is a `tsc` error**.
- `src/ui/src/i18n/index.ts` initialises i18next (`fallbackLng: 'en-GB'`, `supportedLngs: ['en-GB','pt-PT']`,
  `nonExplicitSupportedLngs: false`, `load: 'currentOnly'`, `escapeValue: false` since React escapes).
  `i18next.d.ts` augments `CustomTypeOptions` with the `en-GB` resources, so **`t('unknown.key')` is a `tsc` error**.
- Components use `react-i18next`'s `useTranslation()` for `t`. A thin `LanguageProvider` / `useLanguage()`
  owns `{ language, setLanguage, formatDate }`: it calls `i18n.changeLanguage`, keeps `localStorage` and
  `<html lang>` in sync (via the `languageChanged` event), and syncs with the API.
- Plurals use i18next's `_one` / `_other` suffixes (driven by `Intl.PluralRules`).
- `setLanguage` updates state + `localStorage` immediately (optimistic), then `PUT /api/me/language` when
  signed in; on failure it keeps the local choice and shows a non-blocking error.
- On sign-in / app load with a token, `GET /api/me` and adopt the server preference.
- `LanguageSwitcher` component (native `<select>` or existing Radix popover; labels are the language's own
  name: "English (UK)", "Português (Portugal)") in the sidebar footer and on the login page.
- All `toLocaleDateString(undefined, …)` / `'default'` calls switch to the active language; `react-day-picker`
  gets the matching `date-fns` locale (`enGB` / `pt`) — `date-fns` is already installed transitively.
- Every user-visible literal in `pages/*`, `components/Layout.tsx`, and `components/ui/*` (e.g. calendar
  aria labels) moves to the catalogues. User-generated content (student/tenant names) is never translated.

### Backend messages shown in the UI
Pages currently display the raw API error body (`"400: …"`), which is English. In V1 the frontend
**stops displaying raw response bodies**: it maps known error codes / HTTP status (400, 401, 403, 404,
409, 429, 5xx) to translated messages, falling back to a translated generic error. Server-side
localization of validator messages is deferred (see Out of Scope / Open Question 2).

### Client-side form validation
So users still see *specific* problems in their language, the **create-student** form validates on the
client before submitting, mirroring the server rules, with messages from the catalogues (`validation.*` keys).
(The UI has no register page yet, so register-form validation is deferred with it.)

- Create student: first name, last name, email required; valid email; date of birth in the past.

Errors show inline next to the field. The server stays authoritative: its validators are unchanged and
still run. If the server rejects something the client accepted (rules drifted), the user sees the
translated generic validation error.

## Tech Stack
Unchanged: .NET 10 Minimal APIs, EF Core 10 + PostgreSQL, React 19 + TS 5.9 + Vite 8, Vitest + RTL.

New frontend dependencies (approved): `i18next` and `react-i18next` (latest stable at implementation time,
exact versions pinned in `package-lock.json`). No other i18n plugins: no language detector, no HTTP backend.
Catalogues are bundled.

## Commands

```bash
# Backend
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~PreferredLanguage"
dotnet ef migrations add AddUserPreferredLanguage -p src/Infrastructure -s src/Api
dotnet format

# Frontend (src/ui)
npm run build    # tsc -b && vite build — also the catalogue-completeness check
npm run lint
npm test         # vitest run
npm run dev
```

## Project Structure

```
src/Application/Common/SupportedLanguages.cs
src/Application/Me/Commands/UpdateMyLanguageCommand{,Handler,Validator}.cs
src/Application/Me/Queries/GetCurrentUserQuery{,Handler}.cs + CurrentUserDto.cs
src/Infrastructure/Identity/ApplicationUser.cs            (+ PreferredLanguage)
src/Infrastructure/Identity/IdentityService.cs            (+ Get/UpdatePreferredLanguage)
src/Infrastructure/Persistence/Migrations/*AddUserPreferredLanguage*
src/Api/Endpoints/MeEndpoints.cs                          (+ request records at bottom)
src/ui/src/i18n/{index.ts, i18next.d.ts, LanguageProvider.tsx, languages.ts, locales/en-GB.ts, locales/pt-PT.ts}
src/ui/src/components/LanguageSwitcher.tsx
src/ui/src/api/meApi.ts
tests/UnitTests/...     (validator + handler)
tests/IntegrationTests/... (MeEndpoints)
```

## Code Style

```ts
// src/ui/src/i18n/locales/en-GB.ts — source of truth
export const enGB = {
  nav: { dashboard: 'Dashboard', students: 'Students', tenants: 'Tenants' },
  auth: { signOut: 'Sign out' },
  errors: { generic: 'Something went wrong. Please try again.' },
} as const;
export type Messages = DeepStringify<typeof enGB>;

// src/ui/src/i18n/locales/pt-PT.ts — must match shape exactly
export const ptPT: Messages = {
  nav: { dashboard: 'Painel', students: 'Alunos', tenants: 'Escolas' },
  auth: { signOut: 'Terminar sessão' },
  errors: { generic: 'Ocorreu um erro. Tente novamente.' },
};

// src/ui/src/i18n/i18next.d.ts — typed keys
import 'i18next';
import type { enGB } from './locales/en-GB';
declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation';
    resources: { translation: typeof enGB };
  }
}

// usage
const { t } = useTranslation();
<NavLink ...>{t('nav.students')}</NavLink>
```

```csharp
// Application/Me/Commands/UpdateMyLanguageCommandValidator.cs
public sealed class UpdateMyLanguageCommandValidator : AbstractValidator<UpdateMyLanguageCommand>
{
    public UpdateMyLanguageCommandValidator() =>
        RuleFor(x => x.Language)
            .Must(SupportedLanguages.All.Contains)
            .WithMessage($"Language must be one of: {string.Join(", ", SupportedLanguages.All)}.");
}
```

Conventions per CLAUDE.md: primary constructors, records, file-scoped namespaces, `CancellationToken`
everywhere, `Result` not exceptions. Portuguese copy uses **European Portuguese** (pt-PT) vocabulary and
spelling ("ecrã", "palavra-passe", "Terminar sessão", "Utilizador"), never Brazilian forms ("tela", "senha", "Usuário").

## Testing Strategy

**Backend (NUnit + AwesomeAssertions + Moq)**
- Unit: `UpdateMyLanguageCommandValidator` accepts `en-GB`/`pt-PT`, rejects `pt-BR`, `en`, `""`, `null`,
  wrong casing (`PT-pt` — decide: reject; exact match only).
- Unit: handler passes the caller's id from `ICurrentUser`/claims, not the request.
- Integration (`WebAppFactory`): `GET /api/me` returns default `en-GB` for a new user; `PUT` then `GET`
  round-trips; invalid language → 400; anonymous → 401; works for `User` role (not just Admin);
  **user A's PUT never changes user B** (seed two users, two tenants, assert B unchanged).
- `ValidationPipelineTests` extended (or a sibling test) so the whitelist provably runs via DI.

**Frontend (Vitest + RTL)**
- Catalogue completeness is enforced by `tsc`; plus a test that no `pt-PT` value equals `''`.
- `LanguageProvider`: precedence order (server > localStorage > browser > default), `<html lang>` updates,
  `setLanguage` persists to localStorage and calls the API only when signed in.
- Test setup (`src/test/setup.ts`) initialises i18next with `en-GB` so existing page tests that assert
  English text keep passing unchanged.
- `LanguageSwitcher` switches rendered text on `Layout` and `LoginPage` without reload.
- `formatDate` produces locale-specific output for both languages.
- Register and create-student forms: each client rule shows its translated message in both languages, and
  submission is blocked while invalid (no API call).
- Existing page tests keep passing (they render under the default `en-GB` provider).

**Manual:** walk every page in both languages via `npm run dev` + Aspire, checking for untranslated
literals, truncated layouts (Portuguese strings are ~20–30% longer), and the date picker.

## Boundaries

- **Always:** add strings to `en-GB` first then `pt-PT`; use `t()` for every user-visible literal; take the
  user id from claims; keep the whitelist enforced in both validator and `IdentityService`; run
  `dotnet test`, `npm run build`, `npm test`, `npm run lint` before committing.
- **Ask first:** adding any npm/NuGet dependency beyond `i18next` / `react-i18next`; any change to the JWT claim set;
  translating backend messages server-side; adding a third language.
- **Never:** accept a user id on `/api/me/*`; translate user-generated content; merge pt-PT copy without
  the repo owner's review; display raw API response bodies to users; invalidate the security stamp on a
  language change.

## Success Criteria

1. A new user's `PreferredLanguage` is `en-GB`; existing users are `en-GB` after migration.
2. `PUT /api/me/language` with `pt-PT` → 204, and `GET /api/me` then returns `pt-PT`; with any other value → 400.
3. `PUT /api/me/language` is available to `User`, `Admin`, and `SuperAdmin`; anonymous → 401.
4. A user can only change their own language (integration test with two users proves it).
5. Switching language in the sidebar re-renders every page's text, `<html lang>`, and date formatting
   without a reload or re-login.
6. Signing in on a fresh browser applies the account's saved language.
7. Login page follows browser language (`pt*` → pt-PT) before sign-in and offers the switcher.
8. `npm run build` fails if `pt-PT.ts` is missing a key present in `en-GB.ts`, or if code calls `t()` with
   a key that doesn't exist.
9. A grep for user-visible string literals in JSX under `pages/` and `components/` finds none outside the
   catalogues (excluding test files and user data).
10. No page displays a raw `"<status>: <body>"` error; errors appear translated.
11. The create-student form shows field-specific validation messages in the active language
    before submitting, matching the server rules.
12. pt-PT copy is reviewed and approved by the repo owner before merge, using Escola / Aluno terminology.
13. All existing backend and frontend tests still pass.

## Out of Scope (Deferred)

- **Register page and its translated client-side validation.** The UI has no register page today
  (`authApi.register` exists but is unrouted). Build both together when registration gets a UI.
- **Server-side localization of API messages** (validator/`Error` descriptions via `IStringLocalizer` +
  `Accept-Language`) — needs structured validation errors first; frontend error mapping covers V1.
- **Tenant/school default language** — new users would inherit the school's language; not needed until a
  school wants a non-English default.
- **Languages beyond en-GB / pt-PT** (e.g. pt-BR, es-ES) — architecture allows it; content work deferred.
- **Translated emails / notifications** — no email sending exists yet.
- **Lazy-loaded catalogues per language** — two small catalogues are cheaper bundled.
- **Localized number/currency formatting** — no money is displayed yet (invoices not implemented).
- **Translation management tooling** (Crowdin, Lokalise, extraction scripts).

## Open Questions

1. ~~**i18n library**~~ — **Resolved:** `i18next` + `react-i18next`. Browser detection stays hand-rolled
   (no `i18next-browser-languagedetector`), because the precedence includes the server preference.
2. ~~**Backend error messages**~~ — **Resolved:** server-side localization deferred; frontend maps
   codes/status to translated messages, **and** the create-student form gets translated client-side
   validation (see "Client-side form validation").
3. ~~**Who translates pt-PT?**~~ — **Resolved:** Claude drafts pt-PT copy; the repo owner (native pt-PT
   speaker) reviews it before merge.
4. ~~**Terminology**~~ — **Resolved:** Tenant → **Escola**, Student → **Aluno** in the pt-PT UI.
5. ~~**Switcher placement**~~ — **Resolved:** sidebar footer + login page only. No settings page in V1.

All open questions are resolved.

## Decisions

- **`PreferredLanguage` is a string language code, not an FK to a `Languages` table.** A language is only
  usable once its catalogue ships in the frontend bundle, so the supported set is defined by code
  (`SupportedLanguages.All`), not data. A table would be a second, unauthoritative list. The whitelist is
  enforced in the validator and again in `IdentityService`. No DB check constraint in V1. Revisit if
  languages become runtime-managed data (per-tenant enablement, DB/TMS-backed translations); migrating
  then is trivial because stored values are already the keys.
- **Library:** `i18next` + `react-i18next` (Open Question 1).
