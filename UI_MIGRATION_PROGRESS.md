# ClinicFlow UI Migration Progress

## Baseline audit — 2026-07-11

- Module: Safety baseline, Stitch inventory, route/API mapping
- Files added: `design-reference/clinicflow-stitch-ui-extracted/**`, `UI_MIGRATION_PROGRESS.md`
- Files modified: none in the Angular source at baseline
- Existing functions preserved: authentication and refresh scheduling, authorization interceptor, route and child guards, role filtering, tenant session context, all current CRUD flows, CSV/PDF actions, subscription and platform payment flows
- APIs connected: existing centralized `ApiService`; no endpoint or contract changes
- Tests completed: `npm install`; production `npm run build`
- Build result: PASS — initial 344.96 kB, no TypeScript or template errors
- Baseline notes: npm reports 15 dependency vulnerabilities (2 low, 3 moderate, 10 high); package versions were not changed. Node 25.9.0 is non-LTS.
- Known issues: previous UI uses a green palette that does not match Stitch; Arabic source labels contain UTF-8 mojibake; no live backend credentials are included for authenticated manual journeys.
- Missing backend requirements: password recovery remains unavailable and is already identified by the UI; no endpoint will be invented.
- Next module: Stitch design tokens and application shell

## Stitch export inventory

- Export type: static Tailwind HTML references, not an application framework
- Screens: login, clinic registration, admin dashboard, doctor dashboard, reception dashboard, patient list, patient profile, medical visit
- Visual system: IBM Plex Sans Arabic, medical blue `#0050cb`, bright blue `#0066ff`, deep navy navigation, white cards on `#faf8ff`, 8px spacing grid, 8–16px radii, restrained shadows
- Layout: 280px desktop RTL sidebar; collapsed navigation on tablet; mobile navigation below 767px; fluid 12-column content grid
- Components shown: top bar, role navigation, metric cards, action buttons, filters, tables, status chips, patient summary, timeline, medical forms, tabs, registration stepper
- Assets: eight reference PNGs; export HTML uses remote Google fonts and Material Symbols. No production data assets are copied into the app.

## Page mapping and migration inventory

| Route | Angular component | Stitch equivalent | Roles | Existing API methods / actions | Migration approach and risk |
|---|---|---|---|---|---|
| `/auth` | `AuthComponent` | Login + clinic registration | Guest | `login`, `registerClinic` | Restyle both real forms; preserve payloads and validation. High auth-contract risk. |
| `/dashboard` | `DashboardComponent` | Admin/Doctor/Reception dashboard | Admin, Doctor, Reception | `appointments`, `patients`, `dailyRevenue` | Role-aware hierarchy using real metrics only. |
| `/patients` | `PatientsComponent`, `PatientsTableComponent` | Patients list | Admin, Doctor, Reception | list/search/create/update/delete, duplicates, timeline, CSV import/export, document upload | Preserve fields omitted by Stitch in the drawer. High responsive-table risk. |
| `/patients/:id/chart` | `PatientChartComponent` | Patient profile | Admin, Doctor, Reception | demographics, chart, timeline, ledger, documents view/download/upload | Adopt summary/tabs/timeline design; preserve finance/documents. |
| `/appointments` | `AppointmentsComponent` | Reception/Doctor dashboard patterns | Admin, Doctor, Reception | daily/range/monthly list, availability, create, status, reschedule, cancellations, online approvals | Apply cards/table/status system; preserve scheduling concurrency. |
| `/online-bookings` | `OnlineBookingsComponent` | Reception queue patterns | Admin, Reception | list, approve, reject, doctors, appointments | Keep as real queue; no static mock screen. |
| `/visits` | `VisitsComponent` | Medical visit | Admin, Doctor | create/get/update/finalize/history/templates | Restyle clinical workspace without altering finalization semantics. |
| `/prescriptions` | `PrescriptionsComponent` | Patient/visit clinical patterns | Admin, Doctor | drugs, interaction check, templates, create/view/PDF/WhatsApp | Keep clinical warnings and PDF actions. |
| `/billing` | `BillingComponent` | Reception dashboard + financial summary | Admin, Reception | create/update/refund payment, daily/monthly revenue, debts, receipt PDF | Preserve money precision and row-version handling. |
| `/billing/invoices[/:id]` | `InvoicesComponent` | Financial cards/table extension | Admin, Reception | create/get invoice, add payment, ledger | Shared design; no Stitch equivalent. |
| `/reports` | `ReportsComponent` | Admin dashboard reporting pattern | Admin, Reception | financial dues, daily/monthly revenue, patients/users | Shared metric/table system and existing exports. |
| `/users` | `UsersComponent` | Admin settings pattern | Admin | list/create/update/deactivate/reset password | Preserve role enums and server validation. |
| `/operations` | `OperationsComponent` | Settings pattern | Admin | tenant status, clinic settings, preferences, password, revenue/debts/cancellations | Merge settings into design system; preserve tenant constraints. |
| `/platform/dashboard` | `PlatformComponent` | Admin dashboard pattern | SuperAdmin | platform summary | Platform-only navigation and data. |
| `/platform/clinics[/:id]` | `PlatformComponent`, `AdminClinicDetailComponent` | Admin list/detail extension | SuperAdmin | list/create/update/suspend/reactivate/details/subscriptions | Preserve platform isolation and clinic status actions. |
| `/platform/plans` | `PlatformComponent` | Admin cards/table extension | SuperAdmin | plan list/create/update/status/delete | Preserve plan limits and enum values. |
| `/platform/subscriptions` | `PlatformComponent` | Admin subscription extension | SuperAdmin | list/renew/check expiry/payments | Preserve renewal history and actual payment values. |
| `/platform/reports` | `PlatformComponent` | Admin analytics pattern | SuperAdmin | platform reports/revenue/payments | Real values only; no fake chart data. |
| `/platform/audit-logs` | `PlatformComponent` | Admin activity pattern | SuperAdmin | audit log list | Compact accessible table. |
| `/platform/settings` | `PlatformComponent` | Settings pattern | SuperAdmin | get/update platform settings | Preserve exact settings contract. |

## Functionality present but absent from Stitch

Online booking approval/rejection, CSV import/export, duplicate-patient detection, patient documents, financial ledger, invoice CRUD, partial/refund payments, prescription PDF/WhatsApp, drug interaction checks, clinical templates, user deactivation/reset, tenant status, preferences/password change, platform plans, subscriptions, actual subscription payments, expiry checks, audit logs, platform settings, and concurrency row versions. All remain in scope and must stay connected.

## Shared component plan

Retain and refine the existing standalone `CfPageHeader`, `CfCard`, `CfMetricCard`, `CfBadge`, `CfAvatar`, `CfButton`, `CfEmptyState`, and `CfConfirmDialog`. The shell supplies desktop sidebar, tablet/mobile navigation, header, profile/session state, and global toast. Shared global patterns cover breadcrumbs/page actions, filters, responsive data tables/cards, form fields, drawers, tabs, timelines, upload and money inputs. Feature-specific appointment, patient summary, subscription, and medical workspace patterns stay within their feature until repetition justifies extraction.

## Design-token plan

Centralize Stitch colors, type scale, 8px spacing, radii, shadows, breakpoints, z-index, states, table/form tokens, RTL/LTR logical properties, focus rings, and an optional dark-token set in `src/styles/_tokens.scss`. Global styles consume aliases so existing feature templates migrate without contract or business-logic edits.

## Risk register

| Risk | Control |
|---|---|
| Authentication / refresh / expiry | Do not change `AuthService`, interceptor, storage key, refresh route, or guards; regression build and browser redirect checks. |
| Permissions | Keep route guards and role-filtered nav; never expose unauthorized entries as disabled. |
| Tenant isolation / disabled tenant / expired subscription | Preserve session tenant context and server responses; visual shell must not bypass middleware outcomes. |
| API mapping and enums | Keep `ApiService`, models, numeric enum payloads, row versions, and request shapes unchanged. |
| Dates / money | Preserve current ISO payloads and Angular formatting; never derive revenue from mock data. |
| RTL / LTR | Use logical CSS properties, document `dir`, non-mirrored semantic icons, and responsive checks. |
| Tables / dialogs / overlays | Horizontal containment plus mobile card/grid fallback; explicit z-index tokens and modal focus semantics. |
| Print / PDF | Do not intercept blob downloads or mutate print/PDF endpoint behavior; isolate screen-only shell styling. |

## Design system, shell, and authentication — 2026-07-11

- Module: Design tokens, shared application shell, responsive navigation, shared UI, login, clinic registration
- Files added: none
- Files modified: `src/styles/_tokens.scss`, `src/styles.scss`, `src/index.html`, shell files, auth files, shared card/metric/header/avatar/empty-state components, dashboard presentation
- Existing functions preserved: login, registration, session storage, automatic refresh, role-based navigation, route guards, tenant display, logout, all existing form payloads and validation
- APIs connected: auth login and onboarding registration through the unchanged `ApiService`
- Tests completed: production build; browser DOM/accessibility inspection; desktop login; desktop registration; 390px mobile login; horizontal overflow check; console error check
- Build result: PASS — zero TypeScript/template errors; shell and auth styles emit warning-level component budgets but remain below the configured error limit
- Known issues: authenticated role journeys require a running seeded backend and credentials; remote IBM Plex font falls back safely when offline
- Missing backend requirements: password-recovery endpoint remains absent and the current helper message is preserved
- Next module: role dashboards, patients, appointments, visits, and shared responsive data patterns

## Clinic feature migration and role regression — 2026-07-11

- Module: Admin/Doctor/Reception dashboards; patients and patient chart; appointments; online bookings; visits; prescriptions; billing; invoices; reports; operations/settings; users
- Files added: none
- Files modified: shared UI presentation, dashboard presentation and role-aware revenue loading, mobile shell containment
- Existing functions preserved: all feature component handlers and centralized API calls, availability, status/reschedule, create/update/finalize, PDF/CSV, document, ledger, payment/refund, reporting, user, preference and settings workflows
- APIs connected: all existing clinic feature endpoints through the unchanged `ApiService`
- Tests completed: live seeded Admin login and route smoke; live Doctor login/navigation/direct guard; live Reception login/navigation/direct guard; patient chart; 375px route overflow checks for nine clinic routes; mobile drawer open/close geometry; console error checks; invalid login server feedback
- Build result: PASS after each implementation group
- Known issues: the export has no dedicated visual references for billing, invoices, reports, users, operations, online bookings, prescriptions, or platform pages; these screens use the shared Stitch-derived system while keeping all fields/actions
- Missing backend requirements: password recovery only
- Next module: final build and report

## Platform feature migration — 2026-07-11

- Module: Super Admin dashboard, clinics/detail, plans, subscriptions/renewal/payments, reports, audit logs, platform settings
- Files added/modified: no business logic; platform templates inherit the migrated tokens, shell, tables, cards, forms, drawers, badges and responsive rules
- Existing functions preserved: create/update/suspend/reactivate clinic, plan CRUD/status, renew/change subscription, record actual payment, expiry check, revenue and outstanding reporting, audit and settings
- APIs connected: existing `/api/platform/**` methods in `ApiService`; contracts unchanged
- Tests completed: production compilation and guarded route/static integration inspection
- Build result: PASS
- Known issues: no seeded SuperAdmin credentials are documented, so live SuperAdmin browser actions were not performed
- Missing backend requirements: none identified for existing platform screens
- Next module: final regression report
