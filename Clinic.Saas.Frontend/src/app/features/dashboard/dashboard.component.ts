import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { Appointment, DailyRevenue, Patient } from '../../core/models';
import { CfBadgeComponent, CfBadgeVariant, CfEmptyStateComponent, CfMetricCardComponent, CfPageHeaderComponent } from '../../shared/ui';

@Component({
  selector: 'app-dashboard',
  imports: [DecimalPipe, RouterLink, CfPageHeaderComponent, CfMetricCardComponent, CfBadgeComponent, CfEmptyStateComponent],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly auth = inject(AuthService);
  readonly today = new Date().toISOString().slice(0, 10);
  readonly appointments = signal<Appointment[]>([]);
  readonly patients = signal<Patient[]>([]);
  readonly revenue = signal<DailyRevenue | null>(null);
  readonly isSuperAdmin = computed(() => this.auth.user()?.role === 'SuperAdmin');

  readonly dashboardTitle = computed(() => {
    const role = this.auth.user()?.role;
    if (role === 'Doctor') return 'قائمة عمل الطبيب اليوم';
    if (role === 'Reception') return 'استقبال العيادة اليوم';
    if (role === 'SuperAdmin') return 'نظرة عامة على المنصة';
    return 'لوحة اليوم';
  });

  readonly dashboardDescription = computed(() => {
    const role = this.auth.user()?.role;
    if (role === 'Doctor') return 'المواعيد والزيارات التي تحتاج متابعة سريرية.';
    if (role === 'Reception') return 'المواعيد والمرضى والمدفوعات التي تحتاج متابعة من الاستقبال.';
    if (role === 'SuperAdmin') return 'مؤشرات تشغيل سريعة قبل الانتقال إلى إدارة المنصة.';
    return 'ملخص سريع لحالة العيادة اليوم.';
  });

  readonly nextAppointments = computed(() => this.appointments().slice(0, 6));
  readonly recentPatients = computed(() => this.patients().slice(0, 5));

  async ngOnInit() {
    if (this.isSuperAdmin()) return;

    await Promise.all([
      firstValueFrom(this.api.appointments(this.today)).then((x) => this.appointments.set(x ?? [])).catch(() => this.appointments.set([])),
      firstValueFrom(this.api.patients()).then((x) => this.patients.set(x ?? [])).catch(() => this.patients.set([])),
      firstValueFrom(this.api.dailyRevenue(this.today)).then((x) => this.revenue.set(x)).catch(() => this.revenue.set(null)),
    ]);
  }

  statusVariant(status: string): CfBadgeVariant {
    const normalized = status?.toLowerCase() ?? '';
    if (normalized.includes('complete') || normalized.includes('مكتمل')) return 'success';
    if (normalized.includes('cancel') || normalized.includes('ملغي')) return 'danger';
    if (normalized.includes('confirm') || normalized.includes('مؤكد')) return 'info';
    if (normalized.includes('no') || normalized.includes('لم يحضر')) return 'warning';
    return 'neutral';
  }
}
