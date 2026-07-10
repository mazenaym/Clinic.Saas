import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import {
  AddInvoicePaymentPayload,
  AdminClinic,
  AdminStats,
  ApiResponse,
  Appointment,
  AppointmentAvailability,
  AuditLog,
  AuthSession,
  ClinicSettings,
  ClinicSubscription,
  CreateClinicSubscriptionPayload,
  CreateInvoicePayload,
  DailyRevenue,
  FinancialDuesReport,
  Invoice,
  OnlineBooking,
  Patient,
  PatientChart,
  PatientDocument,
  PatientDocumentUploadMetadata,
  PatientDocumentUploadResult,
  PatientFinancialLedger,
  PatientTimelineItem,
  Prescription,
  Procedure,
  PlatformDashboardSummary,
  PlatformPlan,
  PlatformReports,
  PlatformSettings,
  TenantSubscriptionStatus,
  TenantSubscription,
  User,
  Visit,
  enumValues,
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  login(payload: { email: string; password: string; subdomain?: string }) {
    return this.post<AuthSession>('/auth/login', payload);
  }

  registerClinic(payload: Record<string, unknown>) {
    return this.post<AuthSession>('/onboarding/register-clinic', payload);
  }

  checkSubdomain(subdomain: string) {
    return this.get<{ subdomain: string; isAvailable: boolean; reason?: string }>('/onboarding/check-subdomain', { subdomain });
  }

  me() {
    return this.get<User>('/users/me');
  }

  users() {
    return this.get<User[]>('/users');
  }

  createUser(payload: Record<string, unknown>) {
    return this.post<User>('/users', payload);
  }

  updateUser(id: string, payload: Record<string, unknown>) {
    return this.put<User>(`/users/${id}`, payload);
  }

  deactivateUser(id: string) {
    return this.post<boolean>(`/users/${id}/deactivate`, {});
  }

  resetUserPassword(id: string, newPassword: string) {
    return this.post<boolean>(`/users/${id}/reset-password`, { newPassword });
  }

  preferences() {
    return this.get<Record<string, unknown>>('/users/me/preferences');
  }

  savePreferences(payload: Record<string, unknown>) {
    return this.put<Record<string, unknown>>('/users/me/preferences', payload);
  }

  changePassword(payload: { currentPassword: string; newPassword: string }) {
    return this.post<boolean>('/auth/change-password', payload);
  }

  patients(term = '') {
    return term ? this.get<Patient[]>('/patients/search', { term }) : this.get<Patient[]>('/patients');
  }

  patient(id: string) {
    return this.get<Patient>(`/patients/${id}`);
  }

  createPatient(payload: Record<string, unknown>) {
    return this.post<Patient>('/patients', payload);
  }

  updatePatient(id: string, payload: Record<string, unknown>) {
    return this.put<Patient>(`/patients/${id}`, payload);
  }

  deletePatient(id: string, rowVersion?: string) {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/patients/${id}`, { headers: this.ifMatch(rowVersion) }).pipe(map((r) => r.data));
  }

  getPatientChart(patientId: string) {
    return this.get<PatientChart>(`/patients/${patientId}/chart`);
  }

  getPatientLedger(patientId: string) {
    return this.get<PatientFinancialLedger>(`/patients/${patientId}/ledger`);
  }

  getPatientTimeline(patientId: string) {
    return this.get<PatientTimelineItem[]>(`/patients/${patientId}/timeline`);
  }

  patientTimeline(id: string) {
    return this.getPatientTimeline(id);
  }

  listPatientDocuments(patientId: string) {
    return this.get<PatientDocument[]>('/patient-documents', { patientId });
  }

  uploadPatientDocument(patientId: string, file: File, metadata?: PatientDocumentUploadMetadata): ReturnType<ApiService['uploadPatientDocumentResult']>;

  uploadPatientDocument(patientId: string, file: File, documentType?: number, description?: string): ReturnType<ApiService['uploadPatientDocumentResult']>;

  uploadPatientDocument(patientId: string, file: File, metadataOrDocumentType: PatientDocumentUploadMetadata | number = 1, description = '') {
    const metadata = typeof metadataOrDocumentType === 'number' ? { documentType: metadataOrDocumentType, description } : metadataOrDocumentType;
    return this.uploadPatientDocumentResult(patientId, file, metadata);
  }

  viewPatientDocument(patientId: string, documentId: string) {
    return this.blob(`/patient-documents/${documentId}/view`, { patientId });
  }

  downloadPatientDocument(patientId: string, documentId: string) {
    return this.blob(`/patient-documents/${documentId}/download`, { patientId });
  }

  private uploadPatientDocumentResult(patientId: string, file: File, metadata: PatientDocumentUploadMetadata = {}) {
    const form = new FormData();
    form.append('file', file);
    form.append('documentType', String(metadata.documentType ?? 1));
    form.append('description', metadata.description ?? '');
    return this.http.post<ApiResponse<PatientDocumentUploadResult>>(`${this.baseUrl}/patient-documents`, form, { params: this.params({ patientId }) }).pipe(map((r) => r.data));
  }

  patientDuplicates(phone?: string, nationalId?: string) {
    return this.get<Patient[]>('/patients/duplicates', { phone: phone ?? '', nationalId: nationalId ?? '' });
  }

  exportPatientsCsv() {
    return this.blob('/patients/export');
  }

  appointments(date: string) {
    return this.get<Appointment[]>('/appointments/daily', { date });
  }

  weeklyAppointments(weekStart: string) {
    return this.get<Appointment[]>('/appointments/weekly', { weekStart });
  }

  monthlyAppointments(year: number, month: number) {
    return this.get<Appointment[]>('/appointments/monthly', { year: String(year), month: String(month) });
  }

  getAppointmentAvailability(doctorId: string, date: string, duration?: number) {
    return this.get<AppointmentAvailability>('/appointments/availability', this.cleanParams({ doctorId, date, duration }));
  }

  createAppointment(payload: Record<string, unknown>) {
    return this.post<Appointment>('/appointments', payload);
  }

  updateAppointmentStatus(id: string, status: number, cancelReason?: string, rowVersion?: string) {
    return this.put<Appointment>(`/appointments/${id}/status`, { status, cancelReason, rowVersion });
  }

  rescheduleAppointment(id: string, payload: Record<string, unknown>) {
    return this.put<boolean>(`/appointments/${id}/reschedule`, payload);
  }

  cancellationReport(from: string, to: string) {
    return this.get<Appointment[]>('/appointments/cancellations', { from, to });
  }

  getOnlineBookings(filters?: Record<string, string | number | boolean | undefined | null>) {
    return this.get<OnlineBooking[]>('/online-bookings', this.cleanParams(filters));
  }

  onlineBookings() {
    return this.get<Record<string, unknown>[]>('/online-bookings');
  }

  approveOnlineBooking(id: string, payload: Record<string, unknown> = {}) {
    return this.post<boolean>(`/online-bookings/${id}/approve`, payload);
  }

  rejectOnlineBooking(id: string, rejectReason: string) {
    return this.post<boolean>(`/online-bookings/${id}/reject`, { rejectReason });
  }

  createVisit(payload: Record<string, unknown>) {
    return this.post<Visit>('/visits', payload);
  }

  visit(id: string) {
    return this.get<Visit>(`/visits/${id}`);
  }

  updateVisit(id: string, payload: Record<string, unknown>) {
    return this.put<boolean>(`/visits/${id}`, payload);
  }

  visitHistory(patientId: string) {
    return this.get<Visit[]>(`/visits/patient/${patientId}`);
  }

  finalizeVisit(id: string, rowVersion?: string) {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/visits/${id}/finalize`, {}, { headers: this.ifMatch(rowVersion) }).pipe(map((r) => r.data));
  }

  clinicalTemplates() {
    return this.get<Record<string, unknown>[]>('/clinical-templates');
  }

  createClinicalTemplate(payload: Record<string, unknown>) {
    return this.post<Record<string, unknown>>('/clinical-templates', payload);
  }

  createPrescription(payload: Record<string, unknown>) {
    return this.post<Prescription>('/prescriptions', payload);
  }

  prescription(id: string) {
    return this.get<Prescription>(`/prescriptions/${id}`);
  }

  prescriptionPdf(id: string) {
    return this.blob(`/prescriptions/${id}/pdf`);
  }

  sendPrescriptionWhatsapp(id: string, rowVersion?: string) {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/prescriptions/${id}/send-whatsapp`, {}, { headers: this.ifMatch(rowVersion) }).pipe(map((r) => r.data));
  }

  checkDrugInteractions(drugNames: string[]) {
    return this.post<Record<string, unknown>[]>('/drug-catalog/interactions', drugNames);
  }

  drugs(term = '') {
    return this.get<Record<string, unknown>[]>('/drug-catalog/drugs', { term });
  }

  procedures(includeInactive = false) {
    return this.get<Procedure[]>('/procedures', { includeInactive: String(includeInactive) });
  }

  createProcedure(payload: Record<string, unknown>) {
    return this.post<Procedure>('/procedures', payload);
  }

  updateProcedure(id: string, payload: Record<string, unknown>) {
    return this.put<Procedure>(`/procedures/${id}`, payload);
  }

  activateProcedure(id: string) {
    return this.post<boolean>(`/procedures/${id}/activate`, {});
  }

  deactivateProcedure(id: string) {
    return this.post<boolean>(`/procedures/${id}/deactivate`, {});
  }

  createPayment(payload: Record<string, unknown>) {
    return this.post('/billing/payments', payload);
  }

  getDailyRevenueReport(date: string) {
    return this.dailyRevenue(date);
  }

  dailyRevenue(date: string) {
    return this.get<DailyRevenue>('/billing/reports/daily-revenue', { date });
  }

  createInvoice(payload: CreateInvoicePayload) {
    return this.post<Invoice>('/invoices', payload);
  }

  getInvoiceById(id: string) {
    return this.get<Invoice>(`/invoices/${id}`);
  }

  addInvoicePayment(invoiceId: string, payload: AddInvoicePaymentPayload) {
    return this.post<Invoice>(`/invoices/${invoiceId}/payments`, payload);
  }

  payment(id: string) {
    return this.get<Record<string, unknown>>(`/billing/payments/${id}`);
  }

  patientPayments(patientId: string) {
    return this.get<Record<string, unknown>[]>(`/billing/patients/${patientId}/payments`);
  }

  updatePayment(id: string, payload: Record<string, unknown>) {
    return this.put<boolean>(`/billing/payments/${id}`, payload);
  }

  refundPayment(id: string, reason?: string, rowVersion?: string) {
    return this.post<boolean>(`/billing/payments/${id}/refund`, { reason, rowVersion });
  }

  receiptPdf(id: string) {
    return this.blob(`/billing/payments/${id}/receipt`);
  }

  debts() {
    return this.get<Record<string, unknown>[]>('/billing/debts');
  }

  monthlyRevenue(year: number, month: number) {
    return this.get<Record<string, unknown>[]>('/billing/reports/monthly-revenue', { year: String(year), month: String(month) });
  }

  getFinancialDuesReport(filters: { from?: string; to?: string; doctorId?: string } = {}) {
    return this.get<FinancialDuesReport>('/reports/financial-dues', this.cleanParams(filters));
  }

  tenantStatus() {
    return this.get<TenantSubscriptionStatus>('/tenant/status');
  }

  clinicSettings() {
    return this.get<ClinicSettings>('/tenant/settings');
  }

  updateClinicSettings(payload: ClinicSettings) {
    return this.put<ClinicSettings>('/tenant/settings', payload as unknown as Record<string, unknown>);
  }

  adminDashboard() {
    return this.get<AdminStats>('/platform/dashboard/summary');
  }

  getPlatformDashboardSummary() {
    return this.get<PlatformDashboardSummary>('/platform/dashboard/summary');
  }

  getClinics(filters: Record<string, string | number | boolean | undefined | null> = {}) {
    return this.get<AdminClinic[]>('/platform/clinics', this.cleanParams(filters));
  }

  getClinicById(id: string) {
    return this.get<AdminClinic>(`/platform/clinics/${id}`);
  }

  createPlatformClinic(payload: Record<string, unknown>) {
    return this.post<AdminClinic>('/platform/clinics', payload);
  }

  updatePlatformClinic(id: string, payload: Record<string, unknown>) {
    return this.put<AdminClinic>(`/platform/clinics/${id}`, payload);
  }

  suspendClinic(id: string, reason: string) {
    return this.http.patch<ApiResponse<boolean>>(`${this.baseUrl}/platform/clinics/${id}/suspend`, { reason }).pipe(map((r) => r.data));
  }

  reactivateClinic(id: string) {
    return this.http.patch<ApiResponse<boolean>>(`${this.baseUrl}/platform/clinics/${id}/reactivate`, {}).pipe(map((r) => r.data));
  }

  getPlans(includeInactive = true) {
    return this.getPlatformPlans(includeInactive);
  }

  getPlatformPlans(includeInactive = true) {
    return this.get<PlatformPlan[]>('/platform/plans', { includeInactive: String(includeInactive) });
  }

  getPlatformPlan(id: string) {
    return this.get<PlatformPlan>(`/platform/plans/${id}`);
  }

  createPlan(payload: Record<string, unknown>) {
    return this.createPlatformPlan(payload);
  }

  createPlatformPlan(payload: Record<string, unknown>) {
    return this.post<PlatformPlan>('/platform/plans', payload);
  }

  updatePlan(id: string, payload: Record<string, unknown>) {
    return this.updatePlatformPlan(id, payload);
  }

  updatePlatformPlan(id: string, payload: Record<string, unknown>) {
    return this.put<PlatformPlan>(`/platform/plans/${id}`, payload);
  }

  deletePlatformPlan(id: string) {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/platform/plans/${id}`).pipe(map((r) => r.data));
  }

  updatePlatformPlanStatus(id: string, isActive: boolean) {
    return this.http.patch<ApiResponse<boolean>>(`${this.baseUrl}/platform/plans/${id}/status`, { isActive }).pipe(map((r) => r.data));
  }

  deactivatePlan(id: string) {
    return this.updatePlatformPlanStatus(id, false);
  }

  activatePlan(id: string) {
    return this.updatePlatformPlanStatus(id, true);
  }

  getSubscriptions(filters: Record<string, string | number | boolean | undefined | null> = {}) {
    return this.get<TenantSubscription[]>('/platform/subscriptions', this.cleanParams(filters));
  }

  renewSubscription(tenantId: string, payload: Record<string, unknown>) {
    const actualPaidAmount = payload['actualPaidAmount'];
    return this.post<TenantSubscription>(`/platform/clinics/${tenantId}/subscription/renew`, {
      tenantId,
      planId: payload['planId'],
      customEndDateUtc: payload['customEndDateUtc'] ?? payload['customEndsAtUtc'] ?? null,
      actualPaidAmount: actualPaidAmount === undefined || actualPaidAmount === null || actualPaidAmount === '' ? null : Number(actualPaidAmount),
      paymentDateUtc: payload['paymentDateUtc'] ?? null,
      paymentMethod: payload['paymentMethod'] || null,
      notes: payload['notes'] || null,
    });
  }

  changePlan(tenantId: string, payload: Record<string, unknown>) {
    return this.renewSubscription(tenantId, payload);
  }

  getExpiringSoonSubscriptions(days = 7) {
    return this.get<Record<string, unknown>[]>('/platform/subscriptions/expiring-soon', { days: String(days) }).pipe(map((rows) => rows.map((row, index) => ({
      id: String(row['subscriptionId'] ?? `${row['subdomain'] ?? 'subscription'}-${index}`),
      tenantId: String(row['tenantId'] ?? ''),
      tenantName: String(row['clinicName'] ?? row['name'] ?? row['subdomain'] ?? ''),
      planId: String(row['plan'] ?? ''),
      planName: String(row['plan'] ?? ''),
      planCode: String(row['plan'] ?? ''),
      status: Number(row['status'] ?? 0),
      startsAtUtc: '',
      endsAtUtc: String(row['endDate'] ?? ''),
      autoRenew: false,
      gracePeriodDays: 0,
      daysRemaining: row['endDate'] ? Math.ceil((new Date(String(row['endDate'])).getTime() - Date.now()) / 86_400_000) : 0,
      isInGracePeriod: false,
    } satisfies TenantSubscription))));
  }

  runSubscriptionExpiryCheck() {
    return this.post<{ checked: number; markedPastDue: number; markedExpired: number; skipped: number; errors: number }>('/platform/subscriptions/check-expiry', {});
  }

  getPlatformAuditLogs(filters: Record<string, string | number | boolean | undefined | null> = {}) {
    return this.get<AuditLog[]>('/platform/audit-logs', this.cleanParams(filters));
  }

  getPlatformReports(filters: Record<string, string | number | boolean | undefined | null> = {}) {
    return this.get<PlatformReports>('/platform/reports/platform', this.cleanParams(filters));
  }

  getPlatformSettings() {
    return this.get<PlatformSettings>('/platform/settings/platform');
  }

  updatePlatformSettings(payload: PlatformSettings) {
    return this.put<PlatformSettings>('/platform/settings/platform', payload as unknown as Record<string, unknown>);
  }

  adminClinics() {
    return this.get<AdminClinic[]>('/platform/clinics');
  }

  createClinic(payload: Record<string, unknown>) {
    return this.post<AdminClinic>('/platform/clinics', payload);
  }

  getClinicDetails(id: string) {
    return this.get<AdminClinic>(`/platform/clinics/${id}`);
  }

  updateClinic(id: string, payload: Record<string, unknown>) {
    return this.put<AdminClinic>(`/platform/clinics/${id}`, payload);
  }

  getClinicSubscriptions(id: string) {
    return this.getClinicDetails(id).pipe(
      map((clinic) =>
        clinic.subscriptionId && clinic.subscriptionStartDate && clinic.subscriptionEndDate
          ? [
              {
                id: clinic.subscriptionId,
                plan: clinic.plan,
                startDate: clinic.subscriptionStartDate,
                endDate: clinic.subscriptionEndDate,
                amountPaid: clinic.subscriptionAmountPaid ?? 0,
                status: clinic.subscriptionStatus ?? 0,
              } satisfies ClinicSubscription,
            ]
          : [],
      ),
    );
  }

  addClinicSubscription(id: string, payload: CreateClinicSubscriptionPayload) {
    return this.post<ClinicSubscription>(`/platform/clinics/${id}/subscription/renew`, payload);
  }

  setClinicStatus(id: string, isActive: boolean) {
    const action = isActive ? 'reactivate' : 'suspend';
    return this.http.patch<ApiResponse<AdminClinic>>(`${this.baseUrl}/platform/clinics/${id}/${action}`, {}).pipe(map((r) => r.data));
  }

  clinicUsageMetrics() {
    return this.get<Record<string, unknown>[]>('/platform/reports/usage');
  }

  subscriptionRevenue() {
    return this.get<Record<string, unknown>[]>('/platform/reports/revenue');
  }

  expiringSubscriptions(days = 14) {
    return this.get<Record<string, unknown>[]>('/platform/subscriptions/expiring-soon', { days: String(days) });
  }

  activityLog() {
    return this.get<AuditLog[]>('/platform/audit-logs');
  }

  private get<T>(path: string, params?: Record<string, string>) {
    return this.http.get<ApiResponse<T>>(`${this.baseUrl}${path}`, { params: this.params(params) }).pipe(map((r) => r.data));
  }

  private post<T>(path: string, payload: unknown) {
    return this.http.post<ApiResponse<T>>(`${this.baseUrl}${path}`, payload).pipe(map((r) => r.data));
  }

  private put<T>(path: string, payload: unknown) {
    return this.http.put<ApiResponse<T>>(`${this.baseUrl}${path}`, payload).pipe(map((r) => r.data));
  }

  private blob(path: string, params?: Record<string, string>) {
    return this.http.get(`${this.baseUrl}${path}`, { params: this.params(params), responseType: 'blob' });
  }

  private cleanParams(params?: Record<string, string | number | boolean | undefined | null>) {
    return Object.entries(params ?? {}).reduce<Record<string, string>>((result, [key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        result[key] = String(value);
      }
      return result;
    }, {});
  }

  private ifMatch(rowVersion?: string) {
    return rowVersion ? new HttpHeaders({ 'If-Match': rowVersion }) : undefined;
  }

  private params(params?: Record<string, string>) {
    let result = new HttpParams();
    Object.entries(params ?? {}).forEach(([key, value]) => (result = result.set(key, value)));
    return result;
  }
}
