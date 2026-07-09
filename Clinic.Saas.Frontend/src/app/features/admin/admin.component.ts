import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AdminClinic, AdminStats, enumValues } from '../../core/models';
import { UiService } from '../../core/ui.service';

@Component({
  selector: 'app-admin',
  imports: [FormsModule, DecimalPipe, RouterLink],
  templateUrl: './admin.component.html',
})
export class AdminComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly ui = inject(UiService);
  readonly stats = signal<AdminStats | null>(null);
  readonly clinics = signal<AdminClinic[]>([]);
  readonly usage = signal<Record<string, unknown>[]>([]);
  readonly revenue = signal<Record<string, unknown>[]>([]);
  readonly expiring = signal<Record<string, unknown>[]>([]);
  readonly activity = signal<Record<string, unknown>[]>([]);
  readonly plans = enumValues.plans;
  readonly suspendedClinics = computed(() => this.stats()?.inactiveClinics ?? this.clinics().filter((clinic) => !clinic.isActive).length);
  readonly expiringSoonCount = computed(() => this.expiring().length);
  readonly recentClinics = computed(() => {
    const fromStats = this.stats()?.recentClinics ?? [];
    return (fromStats.length ? fromStats : this.clinics()).slice(0, 5);
  });
  readonly monthlySubscriptionRevenue = computed(() => {
    const monthlyRevenue = this.stats()?.monthlyRevenue;
    if (typeof monthlyRevenue === 'number') return monthlyRevenue;
    return this.revenue().reduce((sum, row) => sum + Number(row['revenue'] ?? 0), 0);
  });
  form: Record<string, any> = {
    name: '', subdomain: '', email: '', phone: '', plan: 1, timeZone: 'Africa/Cairo', currency: 'EGP',
    ownerFullName: '', ownerEmail: '', ownerPassword: '', ownerPhone: '', subscriptionAmountPaid: 0,
    subscriptionStatus: 4, openTime: '09:00:00', closeTime: '21:00:00', slotDurationMin: 20, consultFee: 0, taxPct: 0,
  };

  async ngOnInit() { await this.load(); }

  async load() {
    await Promise.all([
      firstValueFrom(this.api.adminDashboard()).then((x) => this.stats.set(x)).catch(() => this.stats.set(null)),
      firstValueFrom(this.api.adminClinics()).then((x) => this.clinics.set(x ?? [])).catch(() => this.clinics.set([])),
      firstValueFrom(this.api.clinicUsageMetrics()).then((x) => this.usage.set(x ?? [])).catch(() => this.usage.set([])),
      firstValueFrom(this.api.subscriptionRevenue()).then((x) => this.revenue.set(x ?? [])).catch(() => this.revenue.set([])),
      firstValueFrom(this.api.expiringSubscriptions()).then((x) => this.expiring.set(x ?? [])).catch(() => this.expiring.set([])),
      firstValueFrom(this.api.activityLog()).then((x) => this.activity.set(x as unknown as Record<string, unknown>[] ?? [])).catch(() => this.activity.set([])),
    ]);
  }

  async create() {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.createClinic(this.form));
      await this.load();
    }, 'تم إنشاء العيادة');
  }

  async toggle(clinic: AdminClinic) {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.setClinicStatus(clinic.id, !clinic.isActive));
      await this.load();
    }, 'تم تحديث حالة العيادة');
  }
}
