import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { AdminClinic, AdminStats, ApiResponse, Appointment, AuditLog, AuthSession, ClinicSettings, DailyRevenue, Patient, PatientTimelineItem, Prescription, TenantSubscriptionStatus, User, Visit } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  login(payload: { email: string; password: string; subdomain?: string }) {
    return this.post<AuthSession>('/Auth/login', payload);
  }

  registerClinic(payload: Record<string, unknown>) {
    return this.post<AuthSession>('/onboarding/register-clinic', payload);
  }

  checkSubdomain(subdomain: string) {
    return this.get<{ subdomain: string; isAvailable: boolean; reason?: string }>('/onboarding/check-subdomain', { subdomain });
  }

  me() {
    return this.get<User>('/Users/me');
  }

  users() {
    return this.get<User[]>('/Users');
  }

  createUser(payload: Record<string, unknown>) {
    return this.post<User>('/Users', payload);
  }

  updateUser(id: string, payload: Record<string, unknown>) {
    return this.put<User>(`/Users/${id}`, payload);
  }

  deactivateUser(id: string) {
    return this.post<boolean>(`/Users/${id}/deactivate`, {});
  }

  resetUserPassword(id: string, newPassword: string) {
    return this.post<boolean>(`/Users/${id}/reset-password`, { newPassword });
  }

  preferences() {
    return this.get<Record<string, unknown>>('/Users/me/preferences');
  }

  savePreferences(payload: Record<string, unknown>) {
    return this.put<Record<string, unknown>>('/Users/me/preferences', payload);
  }

  changePassword(payload: { currentPassword: string; newPassword: string }) {
    return this.post<boolean>('/Auth/change-password', payload);
  }

  patients(term = '') {
    return term ? this.get<Patient[]>('/Patients/search', { term }) : this.get<Patient[]>('/Patients');
  }

  patient(id: string) {
    return this.get<Patient>(`/Patients/${id}`);
  }

  createPatient(payload: Record<string, unknown>) {
    return this.post<Patient>('/Patients', payload);
  }

  updatePatient(id: string, payload: Record<string, unknown>) {
    return this.put<Patient>(`/Patients/${id}`, payload);
  }

  deletePatient(id: string) {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/Patients/${id}`).pipe(map((r) => r.data));
  }

  patientTimeline(id: string) {
    return this.get<PatientTimelineItem[]>(`/patients/${id}/timeline`);
  }

  patientDuplicates(phone?: string, nationalId?: string) {
    return this.get<Patient[]>('/patients/duplicates', { phone: phone ?? '', nationalId: nationalId ?? '' });
  }

  uploadPatientDocument(patientId: string, file: File, documentType = 1, description = '') {
    const form = new FormData();
    form.append('file', file);
    form.append('documentType', String(documentType));
    form.append('description', description);
    return this.http.post<ApiResponse<Record<string, unknown>>>(`${this.baseUrl}/PatientDocuments`, form, { params: this.params({ patientId }) }).pipe(map((r) => r.data));
  }

  exportPatientsUrl() {
    return `${this.baseUrl}/patients/export`;
  }

  appointments(date: string) {
    return this.get<Appointment[]>('/Appointments/daily', { date });
  }

  weeklyAppointments(weekStart: string) {
    return this.get<Appointment[]>('/appointments/weekly', { weekStart });
  }

  monthlyAppointments(year: number, month: number) {
    return this.get<Appointment[]>('/appointments/monthly', { year: String(year), month: String(month) });
  }

  createAppointment(payload: Record<string, unknown>) {
    return this.post<Appointment>('/Appointments', payload);
  }

  updateAppointmentStatus(id: string, status: number, cancelReason?: string) {
    return this.put<Appointment>(`/Appointments/${id}/status`, { status, cancelReason });
  }

  rescheduleAppointment(id: string, payload: Record<string, unknown>) {
    return this.put<boolean>(`/appointments/${id}/reschedule`, payload);
  }

  cancellationReport(from: string, to: string) {
    return this.get<Appointment[]>('/appointments/cancellations', { from, to });
  }

  onlineBookings() {
    return this.get<Record<string, unknown>[]>('/online-bookings');
  }

  approveOnlineBooking(id: string) {
    return this.post<boolean>(`/online-bookings/${id}/approve`, {});
  }

  rejectOnlineBooking(id: string, rejectReason: string) {
    return this.post<boolean>(`/online-bookings/${id}/reject`, { rejectReason });
  }

  createVisit(payload: Record<string, unknown>) {
    return this.post<Visit>('/Visits', payload);
  }

  visit(id: string) {
    return this.get<Visit>(`/Visits/${id}`);
  }

  updateVisit(id: string, payload: Record<string, unknown>) {
    return this.put<boolean>(`/visits/${id}`, payload);
  }

  visitHistory(patientId: string) {
    return this.get<Visit[]>(`/visits/patient/${patientId}`);
  }

  finalizeVisit(id: string) {
    return this.post<boolean>(`/visits/${id}/finalize`, {});
  }

  clinicalTemplates() {
    return this.get<Record<string, unknown>[]>('/clinical-templates');
  }

  createClinicalTemplate(payload: Record<string, unknown>) {
    return this.post<Record<string, unknown>>('/clinical-templates', payload);
  }

  createPrescription(payload: Record<string, unknown>) {
    return this.post<Prescription>('/Prescriptions', payload);
  }

  prescription(id: string) {
    return this.get<Prescription>(`/Prescriptions/${id}`);
  }

  prescriptionPdfUrl(id: string) {
    return `${this.baseUrl}/prescriptions/${id}/pdf`;
  }

  sendPrescriptionWhatsapp(id: string) {
    return this.post<boolean>(`/prescriptions/${id}/send-whatsapp`, {});
  }

  checkDrugInteractions(drugNames: string[]) {
    return this.post<Record<string, unknown>[]>('/drugcatalog/interactions', drugNames);
  }

  drugs(term = '') {
    return this.get<Record<string, unknown>[]>('/drugcatalog/drugs', { term });
  }

  createPayment(payload: Record<string, unknown>) {
    return this.post('/Billing/payments', payload);
  }

  dailyRevenue(date: string) {
    return this.get<DailyRevenue>('/Billing/reports/daily-revenue', { date });
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

  refundPayment(id: string, reason?: string) {
    return this.post<boolean>(`/billing/payments/${id}/refund`, { reason });
  }

  receiptPdfUrl(id: string) {
    return `${this.baseUrl}/billing/payments/${id}/receipt`;
  }

  debts() {
    return this.get<Record<string, unknown>[]>('/billing/debts');
  }

  monthlyRevenue(year: number, month: number) {
    return this.get<Record<string, unknown>[]>('/billing/reports/monthly-revenue', { year: String(year), month: String(month) });
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
    return this.get<AdminStats>('/admin/dashboard');
  }

  adminClinics() {
    return this.get<AdminClinic[]>('/admin/clinics');
  }

  createClinic(payload: Record<string, unknown>) {
    return this.post<AdminClinic>('/admin/clinics', payload);
  }

  setClinicStatus(id: string, isActive: boolean) {
    return this.http.patch<ApiResponse<AdminClinic>>(`${this.baseUrl}/admin/clinics/${id}/status`, { isActive }).pipe(map((r) => r.data));
  }

  clinicUsageMetrics() {
    return this.get<Record<string, unknown>[]>('/admin/usage');
  }

  subscriptionRevenue() {
    return this.get<Record<string, unknown>[]>('/admin/subscription-revenue');
  }

  expiringSubscriptions(days = 14) {
    return this.get<Record<string, unknown>[]>('/admin/expiring-subscriptions', { days: String(days) });
  }

  activityLog() {
    return this.get<AuditLog[]>('/admin/activity-log');
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

  private params(params?: Record<string, string>) {
    let result = new HttpParams();
    Object.entries(params ?? {}).forEach(([key, value]) => (result = result.set(key, value)));
    return result;
  }
}
