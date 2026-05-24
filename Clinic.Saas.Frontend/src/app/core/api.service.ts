import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { AdminClinic, AdminStats, ApiResponse, Appointment, AuthSession, DailyRevenue, Patient, Prescription, User, Visit } from './models';

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

  appointments(date: string) {
    return this.get<Appointment[]>('/Appointments/daily', { date });
  }

  createAppointment(payload: Record<string, unknown>) {
    return this.post<Appointment>('/Appointments', payload);
  }

  updateAppointmentStatus(id: string, status: number, cancelReason?: string) {
    return this.put<Appointment>(`/Appointments/${id}/status`, { status, cancelReason });
  }

  createVisit(payload: Record<string, unknown>) {
    return this.post<Visit>('/Visits', payload);
  }

  visit(id: string) {
    return this.get<Visit>(`/Visits/${id}`);
  }

  createPrescription(payload: Record<string, unknown>) {
    return this.post<Prescription>('/Prescriptions', payload);
  }

  prescription(id: string) {
    return this.get<Prescription>(`/Prescriptions/${id}`);
  }

  createPayment(payload: Record<string, unknown>) {
    return this.post('/Billing/payments', payload);
  }

  dailyRevenue(date: string) {
    return this.get<DailyRevenue>('/Billing/reports/daily-revenue', { date });
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
