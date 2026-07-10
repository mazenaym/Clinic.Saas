# API route migration

This inventory was re-audited against commit `cb1cce9`. All 38 former Operations actions have an existing canonical owner and no production frontend caller remained. `OperationsController` was therefore removed instead of retaining a God Controller or duplicating it across compatibility controllers. The three `Admin*` controllers remain temporarily, are marked obsolete, emit `Deprecation: true`, contain no SQL, and call the same Application handlers/services used by the platform surface where their contracts are equivalent.

| Old Method | Old Route | Canonical Method | Canonical Route | Old Controller | Canonical Controller | Application Handler/Service | Frontend Callers Updated | Legacy Compatibility Retained | Notes |
|---|---|---|---|---|---|---|---|---|---|
| PUT/POST/GET | `/api/operations/users/{id}`, `.../deactivate`, `.../reset-password`, `.../me/preferences` | same | `/api/users/{id}`, `.../deactivate`, `.../reset-password`, `.../me/preferences` | Operations | Users | UpdateUser, DeactivateUser, ResetUserPassword, Get/SaveUserPreferences | Yes | No | Removed after caller audit; Admin authorization preserved |
| GET/PUT | `/api/operations/tenant/status`, `/settings` | same | `/api/tenant/status`, `/settings` | Operations | Tenant | GetTenantStatus, Get/UpdateClinicSettings | Yes | No | Removed after caller audit; current tenant only |
| GET | `/api/operations/patients/{id}/timeline`, `/duplicates`, `/export` | GET | `/api/patients/{id}/timeline`, `/duplicates`, `/export` | Operations | Patients | GetPatientTimeline, FindPatientDuplicates, ExportPatients | Yes | No | Removed after caller audit |
| GET/PUT | `/api/operations/appointments/weekly`, `/monthly`, `/{id}/reschedule`, `/cancellations` | same | `/api/appointments/weekly`, `/monthly`, `/{id}/reschedule`, `/cancellations` | Operations | Appointments | GetAppointmentRange, RescheduleAppointment, GetAppointmentCancellations | Yes | No | Removed after caller audit; conflict rules unchanged |
| GET/POST | `/api/operations/online-bookings`, `/{id}/approve`, `/{id}/reject` | same | `/api/online-bookings`, `/{id}/approve`, `/{id}/reject` | Operations | OnlineBookings | Get/Approve/RejectOnlineBooking | Yes | No | Removed after caller audit |
| GET/PUT/POST | `/api/operations/visits/patient/{patientId}`, `/{id}`, `/{id}/finalize` | same | `/api/visits/patient/{patientId}`, `/{id}`, `/{id}/finalize` | Operations | Visits | GetPatientVisits, UpdateVisit, FinalizeVisit | Yes | No | Removed after caller audit |
| GET/POST | `/api/operations/clinical-templates` | same | `/api/clinical-templates` | Operations | ClinicalTemplates | Get/CreateClinicalTemplate | Yes | No | Removed after caller audit |
| GET/POST | `/api/operations/prescriptions/{id}/pdf`, `/{id}/send-whatsapp` | same | `/api/prescriptions/{id}/pdf`, `/{id}/send-whatsapp` | Operations | Prescriptions | GetPrescriptionPdf, SendPrescriptionWhatsapp | Yes | No | Removed after caller audit |
| GET | `/api/operations/drugs` | GET | `/api/drug-catalog/drugs` | Operations | DrugCatalog | SearchDrugs | Yes | No | Removed after caller audit |
| POST | `/api/operations/prescriptions/check-interactions` | POST | `/api/drug-catalog/interactions` | Operations | DrugCatalog | CheckDrugInteractions | Yes | No | Removed after caller audit |
| GET/PUT/POST | `/api/operations/billing/payments/{id}`, `/patients/{patientId}/payments`, `/{id}/refund`, `/{id}/receipt`, `/debts`, `/reports/monthly-revenue` | same | `/api/billing/payments/{id}`, `/patients/{patientId}/payments`, `/{id}/refund`, `/{id}/receipt`, `/debts`, `/reports/monthly-revenue` | Operations | Billing | Payment query/command handlers | Yes | No | Removed after caller audit; financial formulas unchanged |
| GET | `/api/operations/admin/usage`, `/subscription-revenue`, `/expiring-subscriptions`, `/activity-log` | GET | `/api/platform/reports/usage`, `/reports/revenue`, `/subscriptions/expiring-soon`, `/audit-logs` | Operations | Platform | Existing admin report queries/platform services | Yes | No | Removed after caller audit |
| GET | `/api/admin/dashboard` | GET | `/api/platform/dashboard/summary` | Admin | Platform | GetAdminDashboard / platform dashboard service | Yes | Yes | One application calculation |
| GET/POST/PUT/PATCH | `/api/admin/clinics`, `/{clinicId}`, `/{clinicId}/status` | same | `/api/platform/clinics`, `/{id}`, `/{id}/suspend`, `/reactivate`, `/disable` | Admin | Platform | Existing clinic commands/queries | Yes | Yes | Generic legacy status retained; frontend maps boolean to suspend/reactivate |
| POST | `/api/admin/clinics/{clinicId}/subscriptions` | POST | `/api/platform/clinics/{tenantId}/subscription/renew` | Admin | Platform | CreateClinicSubscription / subscription service | Yes | Yes | Legacy request contract retained |
| POST | `/api/admin/bootstrap` | POST | legacy only | legacy only | Admin | Admin | BootstrapSuperAdmin | N/A | Yes | Purpose/production dependency not proven; authorization retained |
| CRUD/PATCH | `/api/admin/plans[/{id}[/status|/activate|/deactivate]]` | same | `/api/platform/plans[/{id}[/status|/activate|/deactivate]]` | AdminPlans | Platform | PlatformPlanService | Yes | Yes | Same service is authoritative |
| GET | `/api/admin/usage`, `/subscription-revenue`, `/expiring-subscriptions`, `/activity-log` | GET | `/api/platform/reports/usage`, `/reports/revenue`, `/subscriptions/expiring-soon`, `/audit-logs` | AdminReports | Platform | Existing admin report queries | Yes | Yes | SuperAdmin rules preserved; activity log also preserves tenant-admin legacy behavior |
| All existing methods | `/api/Auth/*` | same | `/api/auth/*` | Auth | Auth | Existing auth commands | Yes | No | Explicit lowercase prefix |
| All existing methods | `/api/Users/*` | same | `/api/users/*` | Users | Users | Existing user handlers | Yes | No | Explicit lowercase prefix |
| All existing methods | `/api/Patients/*` | same | `/api/patients/*` | Patients | Patients | Existing patient handlers | Yes | No | Explicit lowercase prefix |
| All existing methods | `/api/PatientDocuments/*` | same | `/api/patient-documents/*` | PatientDocuments | PatientDocuments | Existing document handlers/storage | Yes | No | Multipart contract unchanged |
| All existing methods | `/api/Appointments/*` | same | `/api/appointments/*` | Appointments | Appointments | Existing appointment handlers | Yes | No | Explicit lowercase prefix |
| All existing methods | `/api/Visits/*` | same | `/api/visits/*` | Visits | Visits | Existing visit handlers | Yes | No | Explicit lowercase prefix |
| All existing methods | `/api/Prescriptions/*` | same | `/api/prescriptions/*` | Prescriptions | Prescriptions | Existing prescription handlers | Yes | No | Explicit lowercase prefix |
| All existing methods | `/api/DrugCatalog/*` | same | `/api/drug-catalog/*` | DrugCatalog | DrugCatalog | Existing catalog handlers | Yes | No | Hyphenated resource name |
| All existing methods | `/api/Billing/*` | same | `/api/billing/*` | Billing | Billing | Existing payment handlers | Yes | No | Explicit lowercase prefix |

## Caller audit

Angular API calls are centralized in `src/app/core/api.service.ts`, with refresh-token handling in `auth.service.ts` and auth URL detection in `auth.interceptor.ts`. All uppercase and legacy admin calls found there were migrated. No Angular `/api/operations` callers were present.

The two diagnostic controllers (`/api/Test/connection` and `/api/Testfactory/session-context`) had no repository callers and exposed database/session internals, so they were removed rather than shipped as production APIs.

## Authorization re-audit

Comparison with `7525f13` found two unrelated widenings. `GET /api/users` was restored to `Admin` plus `UsersManage`; monthly and daily billing revenue reports were restored to `Admin` plus `ReportsFinancialView`. Platform routes remain `SuperAdmin` only. Legacy Admin controllers are equally protected except the intentionally pre-existing tenant-aware activity-log contract (`Admin,SuperAdmin`).

## Platform controller architecture

The former `PlatformController` was removed. Its actions now have these focused owners:

| Former action group | Focused controller | Route prefix |
|---|---|---|
| Dashboard summary | `PlatformDashboardController` | `/api/platform/dashboard` |
| Clinic list/detail/create/update/status | `PlatformClinicsController` | `/api/platform/clinics` |
| Plan CRUD and activation | `PlatformPlansController` | `/api/platform/plans` |
| Subscription list/detail/expiry and clinic subscription commands | `PlatformSubscriptionsController` | `/api/platform/subscriptions` plus preserved absolute clinic-subscription routes |
| Revenue, subscriptions, clinic growth, usage, platform report | `PlatformReportsController` | `/api/platform/reports` |
| Platform settings | `PlatformSettingsController` | `/api/platform/settings` |
| Platform audit logs | `PlatformAuditLogsController` | `/api/platform/audit-logs` |

Shared Application facades coordinate existing handlers/services for dashboard, clinics, plans, reports, and audit logs. Legacy Admin controllers use these same facades and contain HTTP adaptation only.
