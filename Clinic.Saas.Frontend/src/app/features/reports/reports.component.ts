import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { DailyRevenue, FinancialDuesPatient, FinancialDuesReport, Patient, User } from '../../core/models';

@Component({
  selector: 'app-reports',
  imports: [DecimalPipe, FormsModule, RouterLink],
  templateUrl: './reports.component.html',
})
export class ReportsComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly today = new Date().toISOString().slice(0, 10);
  readonly duesReport = signal<FinancialDuesReport | null>(null);
  readonly dailyRevenue = signal<DailyRevenue | null>(null);
  readonly monthlyRevenue = signal<Record<string, unknown>[]>([]);
  readonly patients = signal<Patient[]>([]);
  readonly users = signal<User[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly exportMessage = 'التصدير غير متاح حاليا لأن نقطة تصدير المستحقات المالية غير مفعلة بعد.';

  filters = {
    from: new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10),
    to: new Date().toISOString().slice(0, 10),
    doctorId: '',
    patientId: '',
    status: 'outstanding',
  };

  readonly doctors = computed(() => this.users().filter((user) => user.role === 'Doctor'));
  readonly filteredPatients = computed(() => {
    const reportPatients = this.duesReport()?.patients ?? [];
    return reportPatients.filter((patient) => {
      const matchesPatient = !this.filters.patientId || patient.patientId === this.filters.patientId;
      const matchesStatus =
        this.filters.status === 'all' ||
        (this.filters.status === 'outstanding' && patient.outstandingAmount > 0) ||
        (this.filters.status === 'paid' && patient.outstandingAmount <= 0);
      return matchesPatient && matchesStatus;
    });
  });
  readonly outstandingDues = computed(() => this.filteredPatients().reduce((sum, patient) => sum + Number(patient.outstandingAmount || 0), 0));
  readonly unpaidInvoices = computed(() => this.filteredPatients().filter((patient) => patient.outstandingAmount > 0).length);
  readonly monthlyRevenueTotal = computed(() => this.monthlyRevenue().reduce((sum, row) => sum + Number(row['paidAmount'] || row['netRevenue'] || row['revenue'] || 0), 0));

  async ngOnInit() {
    await Promise.all([
      firstValueFrom(this.api.patients()).then((patients) => this.patients.set(patients ?? [])).catch(() => this.patients.set([])),
      firstValueFrom(this.api.users()).then((users) => this.users.set(users ?? [])).catch(() => this.users.set([])),
      this.loadReports(),
    ]);
  }

  async loadReports() {
    this.loading.set(true);
    this.error.set('');
    try {
      const month = new Date(this.filters.from || this.today);
      const [dues, daily, monthly] = await Promise.all([
        firstValueFrom(this.api.getFinancialDuesReport({
          from: this.filters.from || undefined,
          to: this.filters.to || undefined,
          doctorId: this.filters.doctorId || undefined,
        })),
        firstValueFrom(this.api.getDailyRevenueReport(this.today)).catch(() => null),
        firstValueFrom(this.api.monthlyRevenue(month.getFullYear(), month.getMonth() + 1)).catch(() => []),
      ]);
      this.duesReport.set(dues);
      this.dailyRevenue.set(daily);
      this.monthlyRevenue.set(monthly ?? []);
    } catch {
      this.error.set('تعذر تحميل التقارير المالية.');
    } finally {
      this.loading.set(false);
    }
  }

  patientChartLink(patient: FinancialDuesPatient) {
    return ['/patients', patient.patientId, 'chart'];
  }

  invoiceLink(patient: FinancialDuesPatient) {
    return ['/billing/invoices'];
  }
}
