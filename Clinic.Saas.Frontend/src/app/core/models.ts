export type Role = 'SuperAdmin' | 'Admin' | 'Doctor' | 'Reception';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: string[];
  statusCode: number;
}

export interface User {
  id: string;
  fullName: string;
  email: string;
  role: Role;
  phone?: string;
  specialty?: string;
  isActive: boolean;
}

export interface Tenant {
  id: string;
  name: string;
  subdomain: string;
  email: string;
  phone?: string;
  logoUrl?: string;
  plan: string;
}

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
  tenant: Tenant;
}

export interface Patient {
  id: string;
  patientCode: string;
  fullName: string;
  phoneNumber: string;
  dateOfBirth?: string;
  age?: number;
  gender: string;
  bloodType?: string;
  email?: string;
  address?: string;
  drugAllergies?: string;
  chronicDiseases?: string;
  insuranceCompany?: string;
  createdAt: string;
  medicalHistory?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  totalVisits?: number;
  lastVisitDate?: string;
  totalPaid?: number;
  totalOutstanding?: number;
}

export interface Appointment {
  id: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: string;
  type: string;
  source: string;
  patientName: string;
  patientPhone: string;
  doctorName: string;
  notes?: string;
  reminderSent: boolean;
}

export interface Visit {
  id: string;
  visitDate: string;
  visitType: string;
  chiefComplaint: string;
  diagnosis?: string;
  diagnosisCode?: string;
  followUpDate?: string;
  patientName: string;
  doctorName: string;
  clinicalNotes?: string;
  prescriptions?: Prescription[];
  payment?: Payment;
}

export interface Prescription {
  id: string;
  createdAt: string;
  notes?: string;
  qrCode?: string;
  pdfUrl?: string;
  sentViaWhatsapp: boolean;
  doctorName: string;
  patientName: string;
  items: PrescriptionItem[];
}

export interface PrescriptionItem {
  id?: string;
  drugName: string;
  dosage: string;
  frequency: string;
  duration: string;
  route?: string;
  instructions?: string;
}

export interface Payment {
  id: string;
  invoiceNumber: string;
  totalAmount: number;
  discountAmount: number;
  paidAmount: number;
  remainingAmount: number;
  paymentMethod: string;
  status: string;
  createdAt: string;
  patientName: string;
}

export interface DailyRevenue {
  date: string;
  totalAppointments: number;
  completedVisits: number;
  grossRevenue: number;
  totalDiscounts: number;
  netRevenue: number;
  cashPayments: number;
  cardPayments: number;
  insurancePayments: number;
}

export interface AdminStats {
  totalClinics: number;
  activeClinics: number;
  inactiveClinics: number;
  totalUsers: number;
  totalPatients: number;
  activeSubscriptions: number;
  trialSubscriptions: number;
  expiredSubscriptions: number;
  totalRevenue: number;
  monthlyRevenue: number;
  todayRevenue: number;
  recentClinics: AdminClinic[];
}

export interface AdminClinic {
  id: string;
  name: string;
  subdomain: string;
  email: string;
  phone?: string;
  plan: number;
  currency: string;
  isActive: boolean;
  usersCount: number;
  patientsCount: number;
  appointmentsCount: number;
  clinicRevenue: number;
  subscriptionStatus?: number;
}

export interface TenantSubscriptionStatus {
  state: string;
  trialEndsAt?: string;
  subscriptionEndsAt?: string;
  maxUsers: number;
  maxPatientsPerMonth: number;
}

export interface ClinicSettings {
  workingDays: string;
  openTime: string;
  closeTime: string;
  slotDurationMin: number;
  consultFee: number;
  smsEnabled: boolean;
  whatsappEnabled: boolean;
  emailEnabled: boolean;
  language: string;
  taxPct: number;
}

export interface PatientTimelineItem {
  type: string;
  id: string;
  date: string;
  title: string;
  details?: string;
}

export interface AuditLog {
  id: number;
  action: string;
  entityName: string;
  entityId?: string;
  newValues?: string;
  createdAt: string;
}

export const enumValues = {
  gender: [
    { value: 1, label: 'ذكر' },
    { value: 2, label: 'أنثى' },
  ],
  roles: [
    { value: 1, label: 'مدير العيادة' },
    { value: 2, label: 'دكتور' },
    { value: 3, label: 'ريسبشن' },
  ],
  appointmentType: [
    { value: 1, label: 'كشف جديد' },
    { value: 2, label: 'متابعة' },
    { value: 3, label: 'طارئ' },
    { value: 4, label: 'عن بعد' },
  ],
  appointmentSource: [
    { value: 1, label: 'ريسبشن' },
    { value: 2, label: 'أونلاين' },
    { value: 3, label: 'هاتف' },
    { value: 4, label: 'حضور مباشر' },
  ],
  appointmentStatus: [
    { value: 1, label: 'مجدول' },
    { value: 2, label: 'مؤكد' },
    { value: 3, label: 'مكتمل' },
    { value: 4, label: 'ملغي' },
    { value: 5, label: 'لم يحضر' },
  ],
  visitType: [
    { value: 1, label: 'جديد' },
    { value: 2, label: 'متابعة' },
    { value: 3, label: 'طارئ' },
    { value: 4, label: 'روتيني' },
  ],
  paymentMethod: [
    { value: 1, label: 'كاش' },
    { value: 2, label: 'كارت' },
    { value: 3, label: 'تحويل بنكي' },
    { value: 4, label: 'تأمين' },
    { value: 5, label: 'مختلط' },
  ],
  serviceType: [
    { value: 1, label: 'كشف' },
    { value: 2, label: 'تحليل' },
    { value: 3, label: 'أشعة' },
    { value: 4, label: 'إجراء' },
    { value: 5, label: 'دواء' },
    { value: 6, label: 'أخرى' },
  ],
  plans: [
    { value: 1, label: 'Starter' },
    { value: 2, label: 'Professional' },
    { value: 3, label: 'Enterprise' },
  ],
};
