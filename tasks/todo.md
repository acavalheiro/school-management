# Todo: User Language & Site Translations — V1

Spec: `spec/SPEC-i18n.md` · Plan: `tasks/plan.md` · Branch: `feature/user-language-translations`

## Phase A: Backend
- [x] T1. Language column: `SupportedLanguages`, `ApplicationUser.PreferredLanguage`, EF config, migration `AddUserPreferredLanguage`
  - Verify: `dotnet build`; migration adds non-null varchar(10) default 'en-GB'
- [x] T2. `GET /api/me`: `ICurrentUserService` (reads `sub`), `GetCurrentUserQuery`, `MeEndpoints`, test infra (`sub`, `X-Test-User`, `SeedUserAsync`)
  - Verify: `dotnet test --filter "FullyQualifiedName~Me"`: default en-GB, 401 anonymous, 404 unknown user
- [x] T3. `PUT /api/me/language`: command + validator + handler + `UpdatePreferredLanguageAsync` (double whitelist, no stamp change)
  - Verify: unit + integration tests (204/400, all roles, A's PUT leaves B unchanged), `ValidationPipelineTests` case
- [x] Checkpoint A: `dotnet test` green
- [ ] Manual: GET/PUT `/api/me` with a real JWT via Aspire (+ anonymous → 401)

## Phase B: Frontend foundation
- [x] T4. i18n core (`i18next`, `react-i18next`), typed catalogues, `LanguageProvider`, `LanguageSwitcher`, translated `Layout`, test setup
  - Verify: `npm test && npm run build && npm run lint`; missing pt-PT key / unknown key fails build
- [x] T5. Login page translated + switcher; detection precedence (localStorage > browser > en-GB)
  - Verify: precedence tests; pt-BR browser shows pt-PT
- [x] T6. Server sync via `meApi` (adopt server language on login/load; PUT only when signed in; non-blocking failure)
  - Verify: mocked-fetch tests for the three scenarios
- [ ] Checkpoint B: frontend checks green; manual reload + fresh-browser sign-in applies saved language

## Phase C: Translate the app
- [ ] T7. `ApiError` + `errorMessage(err, t)` status mapping; replace raw `e.message` displays
- [ ] T8. Dashboard + Students: translated, locale dates, date-fns calendar locale, create-student client validation
- [ ] T9. Tenants + TenantDetail: translated (Escola), dates, dialogs
- [ ] T10. `components/ui/*` labels + leftover-literal sweep + "no empty pt-PT value" test
- [ ] Checkpoint C: full manual walkthrough in both languages; **pt-PT copy reviewed by repo owner**

## Phase D: Wrap-up
- [ ] T11. Update CLAUDE.md (`/api/me`, `ICurrentUserService`, i18n conventions, stale frontend-test note, `X-Test-User`); fold outcome into spec
