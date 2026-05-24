import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ClinicSettings, TenantSubscriptionStatus } from '../../core/models';
import { UiService } from '../../core/ui.service';

type Tab = 'reports' | 'profile' | 'inventory' | 'labs' | 'settings' | 'portal';

@Component({
  selector: 'app-operations',
  imports: [FormsModule],
  templateUrl: './operations.component.html',
})
export class OperationsComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly ui = inject(UiService);
  readonly tab = signal<Tab>('reports');
  readonly tenantStatus = signal<TenantSubscriptionStatus | null>(null);
  readonly monthlyRevenue = signal<Record<string, unknown>[]>([]);
  readonly debts = signal<Record<string, unknown>[]>([]);
  readonly cancellations = signal<Record<string, unknown>[]>([]);
  readonly preferences = signal<Record<string, unknown> | null>(null);

  readonly tabs: { id: Tab; label: string }[] = [
    { id: 'reports', label: 'التقارير' },
    { id: 'profile', label: 'الحساب' },
    { id: 'inventory', label: 'المخزون' },
    { id: 'labs', label: 'التحاليل' },
    { id: 'settings', label: 'الإعدادات' },
    { id: 'portal', label: 'الحجز الأونلاين' },
  ];

  inventory = [
    { name: 'سرنجات 5ml', category: 'مستلزمات', quantity: 12, min: 20, expiry: '2026-08-01' },
    { name: 'باراسيتامول', category: 'أدوية', quantity: 8, min: 10, expiry: '2026-11-15' },
  ];

  labs = [
    { test: 'CBC', patient: 'مريض تجريبي', status: 'مطلوب', result: '-' },
    { test: 'HbA1c', patient: 'مريض تجريبي', status: 'قيد المعالجة', result: '-' },
  ];

  settings: ClinicSettings = {
    workingDays: '0111110',
    openTime: '09:00:00',
    closeTime: '21:00:00',
    slotDurationMin: 20,
    consultFee: 300,
    smsEnabled: false,
    whatsappEnabled: true,
    emailEnabled: false,
    language: 'ar',
    taxPct: 0,
  };

  passwordForm = { currentPassword: '', newPassword: '' };
  preferenceForm: Record<string, unknown> = { language: 'ar', theme: 'light', avatarUrl: '' };

  async ngOnInit() {
    await this.load();
  }

  async load() {
    const now = new Date();
    const start = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
    const end = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString();
    await Promise.all([
      firstValueFrom(this.api.tenantStatus()).then((x) => this.tenantStatus.set(x)).catch(() => this.tenantStatus.set(null)),
      firstValueFrom(this.api.clinicSettings()).then((x) => (this.settings = x)).catch(() => undefined),
      firstValueFrom(this.api.monthlyRevenue(now.getFullYear(), now.getMonth() + 1)).then((x) => this.monthlyRevenue.set(x ?? [])).catch(() => this.monthlyRevenue.set([])),
      firstValueFrom(this.api.debts()).then((x) => this.debts.set(x ?? [])).catch(() => this.debts.set([])),
      firstValueFrom(this.api.cancellationReport(start, end)).then((x) => this.cancellations.set(x as unknown as Record<string, unknown>[])).catch(() => this.cancellations.set([])),
      firstValueFrom(this.api.preferences()).then((x) => {
        this.preferences.set(x);
        this.preferenceForm = { ...this.preferenceForm, ...x };
      }).catch(() => undefined),
    ]);
  }

  async saveSettings() {
    await this.ui.run(async () => {
      this.settings = await firstValueFrom(this.api.updateClinicSettings(this.settings));
    }, 'تم حفظ إعدادات العيادة');
  }

  async changePassword() {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.changePassword(this.passwordForm));
      this.passwordForm = { currentPassword: '', newPassword: '' };
    }, 'تم تغيير كلمة المرور');
  }

  async savePreferences() {
    await this.ui.run(async () => {
      const prefs = await firstValueFrom(this.api.savePreferences(this.preferenceForm));
      this.preferences.set(prefs);
    }, 'تم حفظ تفضيلات الحساب');
  }
}
