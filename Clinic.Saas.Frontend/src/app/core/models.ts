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
  rowVersion?: string;
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
  rowVersion?: string;
}

export interface TimeSlot {
  startTime: string;
  endTime: string;
  isAvailable: boolean;
  displayText: string;
}

export interface AppointmentAvailability {
  doctorId: string;
  date: string;
  availableSlots: TimeSlot[];
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
  rowVersion?: string;
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
  rowVersion?: string;
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
  rowVersion?: string;
}

export interface PatientChart {
  patient: PatientChartDemographics;
  medicalWarnings: PatientChartMedicalWarnings;
  recentVisits: PatientChartVisit[];
  recentPrescriptions: PatientChartPrescriptionSummary[];
  recentAppointments: PatientChartAppointment[];
  paymentSummary: PatientChartPaymentSummary;
  documents: PatientChartDocument[];
  timeline: PatientTimelineItem[];
}

export interface PatientChartDemographics {
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
  insuranceCompany?: string;
  createdAt: string;
  rowVersion?: string;
}

export interface PatientChartMedicalWarnings {
  drugAllergies?: string;
  chronicDiseases?: string;
}

export interface PatientChartVisit {
  id: string;
  visitDate: string;
  visitType: string;
  chiefComplaint: string;
  diagnosis?: string;
  diagnosisCode?: string;
  doctorName: string;
  rowVersion?: string;
}

export interface PatientChartPrescriptionSummary {
  id: string;
  createdAt: string;
  doctorName: string;
  itemCount: number;
  itemsSummary?: string;
  sentViaWhatsapp: boolean;
  rowVersion?: string;
}

export interface PatientChartAppointment {
  id: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: string;
  type: string;
  doctorName: string;
  notes?: string;
  rowVersion?: string;
}

export interface PatientChartPaymentSummary {
  invoiceCount: number;
  totalPaid: number;
  totalOutstanding: number;
  lastPaymentAt?: string;
}

export interface PatientChartDocument {
  id: string;
  visitId?: string;
  fileName: string;
  fileSizeKb: number;
  fileType: string;
  documentType: string;
  description?: string;
  uploadedAt: string;
  rowVersion?: string;
}

export interface PatientFinancialLedger {
  summary: PatientFinancialLedgerSummary;
  entries: PatientFinancialLedgerEntry[];
}

export interface PatientFinancialLedgerSummary {
  totalInvoiced: number;
  totalPaid: number;
  outstandingBalance: number;
}

export interface PatientFinancialLedgerEntry {
  date: string;
  type: string;
  referenceNumber: string;
  description: string;
  debit: number;
  credit: number;
  balance: number;
}

export interface PatientDocument {
  id: string;
  patientId: string;
  fileName: string;
  fileSizeKb: number;
  fileType: string;
  documentType: number;
  description?: string;
  uploadedBy?: string;
  uploadedByName?: string;
  uploadedAt: string;
  rowVersion?: string;
}

export interface PatientDocumentUploadResult {
  id: string;
  fileName: string;
  fileSizeKb: number;
  fileType: string;
  documentType: number;
  description?: string;
  uploadedAt: string;
}

export interface PatientDocumentUploadMetadata {
  documentType?: number;
  description?: string;
}

export interface Invoice {
  id: string;
  patientId: string;
  visitId?: string;
  invoiceNumber: string;
  patientName: string;
  subtotal: number;
  discountAmount: number;
  taxAmount: number;
  grandTotal: number;
  paidAmount: number;
  remainingAmount: number;
  status: string;
  notes?: string;
  createdAt: string;
  updatedAt: string;
  rowVersion?: string;
  items: InvoiceItem[];
  payments: InvoicePayment[];
}

export interface InvoiceItem {
  id: string;
  procedureId?: string;
  description: string;
  serviceType?: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  taxAmount: number;
  lineTotal: number;
  sortOrder: number;
}

export interface InvoicePayment {
  id: string;
  amount: number;
  paymentMethod: string;
  paymentReference?: string;
  notes?: string;
  paidAt: string;
}

export interface CreateInvoicePayload {
  patientId: string;
  visitId?: string;
  notes?: string;
  items: CreateInvoiceItemPayload[];
}

export interface CreateInvoiceItemPayload {
  procedureId?: string;
  description: string;
  serviceType?: number;
  quantity: number;
  unitPrice: number;
  discountAmount?: number;
  taxAmount?: number;
}

export interface AddInvoicePaymentPayload {
  amount: number;
  paymentMethod: number;
  paymentReference?: string;
  notes?: string;
}

export interface Procedure {
  id: string;
  categoryId?: string;
  categoryName?: string;
  name: string;
  specialty?: string;
  defaultPrice: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
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

export interface FinancialDuesReport {
  summary: FinancialDuesSummary;
  patients: FinancialDuesPatient[];
}

export interface FinancialDuesSummary {
  totalOutstanding: number;
  totalPaid: number;
  patientsWithDebtCount: number;
}

export interface FinancialDuesPatient {
  patientId: string;
  patientName: string;
  phone?: string;
  totalAmount: number;
  paidAmount: number;
  outstandingAmount: number;
  lastPaymentDate?: string;
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

export interface PlatformPlan {
  id: string;
  name: string;
  code: string;
  description?: string;
  price: number;
  currency: string;
  durationDays: number;
  maxUsers?: number;
  maxPatients?: number;
  maxDoctors?: number;
  featuresJson?: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string;
}

export interface TenantSubscription {
  id: string;
  tenantId: string;
  tenantName: string;
  planId: string;
  planName: string;
  planCode: string;
  status: number;
  startsAtUtc: string;
  endsAtUtc: string;
  renewedAtUtc?: string;
  cancelledAtUtc?: string;
  suspendedAtUtc?: string;
  autoRenew: boolean;
  gracePeriodDays: number;
  lastCheckedAtUtc?: string;
  notes?: string;
  daysRemaining: number;
  isInGracePeriod: boolean;
  actualPaidAmount?: number;
  paymentDateUtc?: string;
  paymentMethod?: string;
}

export interface PlatformDashboardSummary {
  totalClinics: number;
  activeClinics: number;
  trialClinics: number;
  expiredClinics: number;
  suspendedClinics: number;
  totalUsers: number;
  totalPatients: number;
  totalAppointments: number;
  monthlySubscriptionRevenue: number;
  annualSubscriptionRevenue: number;
  expiringSoonCount: number;
  expiredSubscriptionsCount: number;
  recentClinics: AdminClinic[];
  subscriptionAlerts: TenantSubscription[];
}

export interface PlatformRevenueReport {
  currentMonthRevenue: number;
  currentYearRevenue: number;
  totalCollected: number;
  paymentCount: number;
  averagePayment: number;
}

export interface PlatformRevenueAnalytics {
  currentMonthRevenue: number; previousMonthRevenue: number; currentMonthChangePercentage: number;
  currentYearRevenue: number; previousYearRevenue: number; currentYearChangePercentage: number;
  fromUtc: string; toUtc: string;
  weeklyRevenue: PlatformWeeklyRevenue[]; monthlyRevenue: PlatformMonthlyRevenue[];
}
export interface PlatformWeeklyRevenue { weekStartUtc: string; weekEndUtc: string; year: number; weekNumber: number; label: string; revenue: number; paymentsCount: number; clinicsCount: number; }
export interface PlatformMonthlyRevenue { year: number; month: number; monthKey: string; monthLabel: string; revenue: number; paymentsCount: number; clinicsCount: number; }

export interface PlatformSubscriptionStatusReport {
  active: number;
  trial: number;
  expired: number;
  suspended: number;
  expiringSoon: number;
}

export interface PlatformSubscriptionPayment {
  id: string;
  tenantId: string;
  clinicName: string;
  subscriptionId?: string;
  planId?: string;
  planName: string;
  amount: number;
  currency: string;
  paymentDateUtc: string;
  paymentMethod?: string;
  notes?: string;
}

export interface PlatformReports {
  revenue: PlatformRevenueReport;
  subscriptions: PlatformSubscriptionStatusReport;
  recentPayments: PlatformSubscriptionPayment[];
}

export interface PlatformSettings {
  trialDurationDays: number;
  expiringSoonThresholdDays: number;
  defaultGracePeriodDays: number;
  autoSuspendExpiredClinics: boolean;
  currencyCode: string;
  platformSupportEmail?: string;
  platformSupportPhone?: string;
  paymentMethodsEnabled?: string;
  taxPercentage?: number;
}

export interface AdminClinic {
  id: string;
  name: string;
  subdomain: string;
  email: string;
  phone?: string;
  logoUrl?: string;
  plan: number;
  timeZone?: string;
  currency: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  usersCount: number;
  patientsCount: number;
  appointmentsCount: number;
  clinicRevenue: number;
  subscriptionId?: string;
  subscriptionStatus?: number;
  subscriptionStartDate?: string;
  subscriptionEndDate?: string;
  subscriptionAmountPaid?: number;
}

export interface ClinicSubscription {
  id: string;
  tenantId?: string;
  plan: number;
  startDate: string;
  endDate: string;
  amountPaid: number;
  status: number;
  paymentRef?: string;
  notes?: string;
  createdAt?: string;
}

export interface CreateClinicSubscriptionPayload {
  plan: number;
  startDate: string;
  endDate: string;
  amountPaid: number;
  status: number;
  paymentRef?: string;
  notes?: string;
}

export interface OnlineBooking {
  id: string;
  patientName: string;
  patientPhone: string;
  patientEmail?: string;
  requestedDate: string;
  requestedTime: string;
  doctorId?: string;
  doctorName?: string;
  complaint?: string;
  status: string;
  confirmCode: string;
  rejectReason?: string;
  createdAt: string;
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
