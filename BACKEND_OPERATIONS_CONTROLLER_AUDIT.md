# OperationsController Tenant And Architecture Audit

Scope: `Clinic.Saas/Controllers/OperationsController.cs`

This is an audit-only document. No code changes were made for this task.

## Executive Summary

`OperationsController` is acting as a large transaction script controller. It injects `DapperContext` directly and runs SQL in almost every endpoint. Most tenant-owned queries include `TenantId = @TenantId`, but the controller still has architecture and security risks because tenant checks are duplicated manually instead of enforced through tenant-aware repositories/use cases and `IDbConnectionFactory`.

Highest-risk areas:

- `PUT /api/operations/users/me/preferences` updates by `UserId` only and does not include `TenantId`.
- `PUT /api/operations/billing/payments/{id}` writes computed columns (`RemainingAmount`, `PaymentItems.TotalPrice`) and deletes/inserts `PaymentItems` by `PaymentId` only.
- `GET /api/operations/prescriptions/{id}/pdf` loads prescription items by `PrescriptionId` only after loading the tenant-scoped prescription. This is lower risk if IDs are globally unique, but should still move behind a repository method.
- Drug endpoints have no tenant check. They may be global catalog data, but this needs an explicit design decision.
- Admin reports intentionally cross tenants for `SuperAdmin`, but should move to admin/report use cases to avoid accidentally exposing tenant data later.

## Endpoints With Direct SQL

Every endpoint below uses direct SQL directly or through a private helper in `OperationsController`.

## users/preferences

| Endpoint | TenantId check? | Direct SQL? | Cross-tenant risk? | Proposed move |
|---|---:|---:|---|---|
| `PUT /api/operations/users/{id}` | Yes, `RequireTenant()` and SQL filters `Users.TenantId = @TenantId`. | Yes | Low tenant risk, but role update logic is controller-owned. | `IUserRepository.UpdateTenantUserAsync` + `UpdateUserCommand` |
| `POST /api/operations/users/{id}/deactivate` | Yes, user lookup/update and active admin count use `TenantId`. | Yes | Low tenant risk. Business invariant "last admin" belongs in use case. | `IUserRepository.DeactivateAsync` + `DeactivateUserCommand` |
| `POST /api/operations/users/{id}/reset-password` | Yes, update filters by `TenantId`. | Yes | Low tenant risk. Password reset should be an application use case with audit and policy. | `IUserRepository.ResetPasswordAsync` + `ResetUserPasswordCommand` |
| `GET /api/operations/users/me/preferences` | No DB access; returns hard-coded preferences. | No | No cross-tenant DB risk, but behavior is fake/incomplete. | `GetUserPreferencesQuery` |
| `PUT /api/operations/users/me/preferences` | Partial. Checks `UserId`, but SQL updates `Users` by `Id = @UserId` only. | Yes | Medium. If user IDs are globally unique this likely works, but tenant-owned updates should still include `TenantId = @TenantId`. | `IUserPreferenceRepository.SaveAsync(tenantId, userId, dto)` + `SaveUserPreferencesCommand` |
| helper `UserById(...)` | Yes, caller passes `tenantId`; SQL filters by tenant. | Yes | Low, but should not live in controller. | `IUserRepository.GetByIdAsync(tenantId, userId)` |

## tenant/settings

| Endpoint | TenantId check? | Direct SQL? | Cross-tenant risk? | Proposed move |
|---|---:|---:|---|---|
| `GET /api/operations/tenant/status` | Yes, `Tenants.Id = @TenantId`; subquery uses tenant id from row. | Yes | Low. | `ITenantRepository.GetSubscriptionStatusAsync` + `GetTenantStatusQuery` |
| `GET /api/operations/tenant/settings` | Yes, via `EnsureSettings(connection, tenantId)`. | Yes | Low. | `IClinicSettingsRepository.GetOrDefaultAsync` + `GetClinicSettingsQuery` |
| `PUT /api/operations/tenant/settings` | Yes, `MERGE` keyed by `TenantId`. | Yes | Low tenant risk. `MERGE` in controller is hard to test and can hide concurrency issues. | `IClinicSettingsRepository.UpsertAsync` + `UpdateClinicSettingsCommand` |
| helper `EnsureSettings(...)` | Yes, filters by `TenantId`. | Yes | Low. | `IClinicSettingsRepository.GetOrDefaultAsync` |

## patient documents

No patient document endpoint exists in `OperationsController`. Patient document APIs are in `PatientDocumentsController`.

Related patient endpoints in `OperationsController` still contain direct SQL and should be moved out:

| Endpoint | TenantId check? | Direct SQL? | Cross-tenant risk? | Proposed move |
|---|---:|---:|---|---|
| `GET /api/operations/patients/{id}/timeline` | Yes, every union branch filters `TenantId` and `PatientId`. | Yes | Low tenant risk, but no upfront patient existence/ownership check. | `IPatientTimelineQueryService.GetTimelineAsync` |
| `GET /api/operations/patients/duplicates` | Yes, filters `Patients.TenantId`. | Yes | Low. | `IPatientRepository.FindDuplicatesAsync` + `FindPatientDuplicatesQuery` |
| `GET /api/operations/patients/export` | Yes, filters `Patients.TenantId`. | Yes | Low tenant risk; high privacy/export governance risk. Needs authorization and audit policy. | `IPatientExportService.ExportCsvAsync` + `ExportPatientsQuery` |

## appointments operations

| Endpoint | TenantId check? | Direct SQL? | Cross-tenant risk? | Proposed move |
|---|---:|---:|---|---|
| `GET /api/operations/appointments/weekly` | Yes, through `AppointmentRange`. | Yes, via helper. | Low. | `IAppointmentRepository.GetRangeAsync` + `GetWeeklyAppointmentsQuery` |
| `GET /api/operations/appointments/monthly` | Yes, through `AppointmentRange`. | Yes, via helper. | Low. | `IAppointmentRepository.GetRangeAsync` + `GetMonthlyAppointmentsQuery` |
| `PUT /api/operations/appointments/{id}/reschedule` | Yes, appointment lookup/conflict/update all filter by `TenantId`. | Yes | Low tenant risk. Business conflict rules duplicate repository logic. | `IAppointmentRepository.RescheduleAsync` + `RescheduleAppointmentCommand` |
| `GET /api/operations/appointments/cancellations` | Yes, filters `TenantId`. | Yes | Low. | `IAppointmentReportRepository.GetCancellationReportAsync` |
| helper `AppointmentRange(...)` | Yes, filters `a.TenantId`. | Yes | Low. | `IAppointmentRepository.GetRangeAsync` |

## online bookings

| Endpoint | TenantId check? | Direct SQL? | Cross-tenant risk? | Proposed move |
|---|---:|---:|---|---|
| `GET /api/operations/online-bookings` | Yes, filters `TenantId`. | Yes | Low. | `IOnlineBookingRepository.GetByTenantAsync` + `GetOnlineBookingsQuery` |
| `POST /api/operations/online-bookings/{id}/approve` | Yes, update filters `TenantId` and `Id`. | Yes | Low. Missing transition/business validation. | `IOnlineBookingRepository.ApproveAsync` + `ApproveOnlineBookingCommand` |
| `POST /api/operations/online-bookings/{id}/reject` | Yes, update filters `TenantId` and `Id`. | Yes | Low. Missing transition/business validation. | `IOnlineBookingRepository.RejectAsync` + `RejectOnlineBookingCommand` |

## visits/clinical templates

| Endpoint | TenantId check? | Direct SQL? | Cross-tenant risk? | Proposed move |
|---|---:|---:|---|---|
| `GET /api/operations/visits/patient/{patientId}` | Yes, filters `TenantId` and `PatientId`. | Yes | Low. | `IVisitRepository.GetByPatientIdAsync` + `GetPatientVisitsQuery` |
| `PUT /api/operations/visits/{id}` | Yes, lock check/update filter `TenantId`. | Yes | Low tenant risk. Clinical update/finalized invariant should be in use case. | `IVisitRepository.UpdateClinicalAsync` + `UpdateVisitCommand` |
| `POST /api/operations/visits/{id}/finalize` | Yes, update filters `TenantId`. | Yes | Low. | `IVisitRepository.FinalizeAsync` + `FinalizeVisitCommand` |
| `GET /api/operations/clinical-templates` | Yes, filters `TenantId`. | Yes | Low. | `IClinicalTemplateRepository.GetActiveAsync` |
| `POST /api/operations/clinical-templates` | Yes, inserts current `TenantId`. | Yes | Low. | `IClinicalTemplateRepository.CreateAsync` + `CreateClinicalTemplateCommand` |

## prescriptions pdf/whatsapp

| Endpoint | TenantId check? | Direct SQL? | Cross-tenant risk? | Proposed move |
|---|---:|---:|---|---|
| `GET /api/operations/prescriptions/{id}/pdf` | Prescription header is tenant-scoped. Items query uses only `PrescriptionId` after header exists. | Yes | Low to medium. Safe if prescription IDs are globally unique and item query only happens after tenant-scoped header. Still not ideal because child table has no tenant predicate. | `IPrescriptionRepository.GetForPdfAsync(tenantId, id)` + `PrescriptionPdfService` |
| `POST /api/operations/prescriptions/{id}/send-whatsapp` | Yes, settings and prescription update use `TenantId`. | Yes | Low. Should verify prescription exists and integration send result instead of only setting flag. | `IPrescriptionRepository.MarkWhatsappSentAsync` + `SendPrescriptionWhatsappCommand` |
| `GET /api/operations/drugs` | No tenant check. | Yes | Depends on design. Low if `Drugs` is global catalog; medium if clinics can have private formularies. | `IDrugCatalogRepository.SearchAsync` |
| `POST /api/operations/prescriptions/check-interactions` | No tenant check. | Yes | Depends on design. Low if global catalog. | `IDrugInteractionService.CheckAsync` |

## billing update/refund/reports

| Endpoint | TenantId check? | Direct SQL? | Cross-tenant risk? | Proposed move |
|---|---:|---:|---|---|
| `GET /api/operations/billing/payments/{id}` | Payment header is tenant-scoped. Items query uses only `PaymentId` after header exists. | Yes | Low to medium. Same child-table pattern as prescriptions. | `IPaymentRepository.GetByIdAsync(tenantId, id)` |
| `GET /api/operations/billing/patients/{patientId}/payments` | Yes, filters `TenantId` and `PatientId`. | Yes | Low. | `IPaymentRepository.GetByPatientAsync` |
| `PUT /api/operations/billing/payments/{id}` | Header update filters `TenantId`. Items delete/insert use only `PaymentId`. | Yes | High. Also updates computed `RemainingAmount` and inserts computed `TotalPrice`; this likely breaks at runtime and bypasses hardened `PaymentRepository`. It also allows changing `PatientId`/`VisitId` without validating they belong to the same tenant. | `IPaymentRepository.UpdateAsync(tenantId, payment)` plus explicit item replacement method, or `UpdatePaymentCommand` using repository transaction |
| `POST /api/operations/billing/payments/{id}/refund` | Yes, update filters `TenantId`. | Yes | Low tenant risk; refund business rules are too thin. | `IPaymentRepository.RefundAsync` + `RefundPaymentCommand` |
| `GET /api/operations/billing/payments/{id}/receipt` | Yes, filters `TenantId`. | Yes | Low. | `IPaymentRepository.GetReceiptDataAsync` + `ReceiptPdfService` |
| `GET /api/operations/billing/debts` | Yes, filters `p.TenantId`. Join is tenant-aware. | Yes | Low. | `IFinancialReportRepository.GetDebtTrackingAsync` |
| `GET /api/operations/billing/reports/monthly-revenue` | Yes, filters `TenantId`. | Yes | Low. | `IFinancialReportRepository.GetMonthlyRevenueAsync` |

## admin reports/activity log

| Endpoint | TenantId check? | Direct SQL? | Cross-tenant risk? | Proposed move |
|---|---:|---:|---|---|
| `GET /api/operations/admin/usage` | No tenant filter by design; `SuperAdmin` only. | Yes | Medium by blast radius. Intended cross-tenant access, but must stay SuperAdmin-only and audited. | `IAdminReportRepository.GetUsageMetricsAsync` |
| `GET /api/operations/admin/subscription-revenue` | No tenant filter by design; `SuperAdmin` only. | Yes | Medium by blast radius. | `IAdminReportRepository.GetSubscriptionRevenueAsync` |
| `GET /api/operations/admin/expiring-subscriptions` | No tenant filter by design; `SuperAdmin` only. | Yes | Medium by blast radius. | `IAdminReportRepository.GetExpiringSubscriptionsAsync` |
| `GET /api/operations/admin/activity-log` | Conditional. SuperAdmin gets all logs; Admin gets `_currentUser.TenantId`. | Yes | Medium. If an Admin somehow has null TenantId, query returns no rows because SQL `TenantId = NULL` is false, but logic should reject missing tenant explicitly. | `IAuditLogRepository.SearchAsync` + `GetActivityLogQuery` |
| helper `Audit(...)` | Uses `_currentUser.TenantId`; no explicit tenant required. | Yes | Low to medium. For tenant actions it logs tenant. For SuperAdmin actions tenant may be null, which is probably acceptable but should be deliberate. | `IAuditWriter.WriteAsync` |

## Direct SQL Inventory By Endpoint

- `PUT /api/operations/users/{id}`: `SELECT Users`, duplicate email `COUNT`, `UPDATE Users`, helper `UserById`.
- `POST /api/operations/users/{id}/deactivate`: `SELECT Users`, active admin `COUNT`, `UPDATE Users`.
- `POST /api/operations/users/{id}/reset-password`: `UPDATE Users`.
- `PUT /api/operations/users/me/preferences`: `UPDATE Users`.
- `GET /api/operations/tenant/status`: `SELECT Tenants` and subscription subquery.
- `GET /api/operations/tenant/settings`: `SELECT ClinicSettings` via helper.
- `PUT /api/operations/tenant/settings`: `MERGE ClinicSettings`, then `SELECT ClinicSettings` via helper.
- `GET /api/operations/patients/{id}/timeline`: `UNION ALL` over `Appointments`, `Visits`, `Prescriptions`, `Payments`.
- `GET /api/operations/patients/duplicates`: `SELECT Patients`.
- `GET /api/operations/patients/export`: `SELECT Patients`.
- `GET /api/operations/appointments/weekly`: `SELECT Appointments` via `AppointmentRange`.
- `GET /api/operations/appointments/monthly`: `SELECT Appointments` via `AppointmentRange`.
- `PUT /api/operations/appointments/{id}/reschedule`: `SELECT Appointments`, conflict `COUNT`, `UPDATE Appointments`.
- `GET /api/operations/appointments/cancellations`: `SELECT Appointments`.
- `GET /api/operations/online-bookings`: `SELECT OnlineBookings`.
- `POST /api/operations/online-bookings/{id}/approve`: `UPDATE OnlineBookings`.
- `POST /api/operations/online-bookings/{id}/reject`: `UPDATE OnlineBookings`.
- `GET /api/operations/visits/patient/{patientId}`: `SELECT Visits`.
- `PUT /api/operations/visits/{id}`: finalized `COUNT`, `UPDATE Visits`.
- `POST /api/operations/visits/{id}/finalize`: `UPDATE Visits`.
- `GET /api/operations/clinical-templates`: `SELECT ClinicalTemplates`.
- `POST /api/operations/clinical-templates`: `INSERT ClinicalTemplates`.
- `GET /api/operations/prescriptions/{id}/pdf`: `SELECT Prescriptions` and `SELECT PrescriptionItems`.
- `POST /api/operations/prescriptions/{id}/send-whatsapp`: `SELECT ClinicSettings`, `UPDATE Prescriptions`.
- `GET /api/operations/drugs`: `SELECT Drugs`.
- `POST /api/operations/prescriptions/check-interactions`: `SELECT Drugs`.
- `GET /api/operations/billing/payments/{id}`: `SELECT Payments`, `SELECT PaymentItems`.
- `GET /api/operations/billing/patients/{patientId}/payments`: `SELECT Payments`.
- `PUT /api/operations/billing/payments/{id}`: `UPDATE Payments`, `DELETE PaymentItems`, `INSERT PaymentItems`.
- `POST /api/operations/billing/payments/{id}/refund`: `UPDATE Payments`.
- `GET /api/operations/billing/payments/{id}/receipt`: `SELECT Payments`.
- `GET /api/operations/billing/debts`: aggregate `SELECT Payments JOIN Patients`.
- `GET /api/operations/billing/reports/monthly-revenue`: aggregate `SELECT Payments`.
- `GET /api/operations/admin/usage`: cross-tenant `SELECT Tenants` with counts.
- `GET /api/operations/admin/subscription-revenue`: cross-tenant `SELECT Subscriptions`.
- `GET /api/operations/admin/expiring-subscriptions`: cross-tenant `SELECT Subscriptions JOIN Tenants`.
- `GET /api/operations/admin/activity-log`: `SELECT AuditLogs`.

## Recommended Refactor Order

1. Fix billing update first because it directly conflicts with computed-column hardening already done in `PaymentRepository`.
2. Move preferences update into a tenant-scoped user preference use case.
3. Move prescription PDF and payment detail child-item reads behind tenant-scoped repository methods.
4. Split reports into `FinancialReports`, `AppointmentReports`, and `AdminReports` query services.
5. Replace controller-private `Audit`, `EnsureSettings`, and PDF generation helpers with application services.
6. Replace `DapperContext` injection in `OperationsController` with use case handlers only, then shrink or delete the controller.
