import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AdminClinic, PlatformDashboardSummary, PlatformMonthlyRevenue, PlatformPlan, PlatformReports, PlatformRevenueAnalytics, PlatformSettings, PlatformWeeklyRevenue, TenantSubscription } from '../../core/models';
import { UiService } from '../../core/ui.service';

@Component({
  selector: 'app-platform',
  imports: [DatePipe, DecimalPipe, FormsModule, RouterLink],
  templateUrl: './platform.component.html',
  styles: [`
    .plans-table-panel {
      align-content: start;
    }

    .plans-table-panel .table-wrap {
      margin-top: 0;
    }

    .plans-table-panel {
      gap: 0;
    }

    .checkbox-line {
      grid-template-columns: auto 1fr;
      align-items: center;
      gap: 10px;
    }

    .checkbox-line input {
      width: auto;
      min-height: auto;
    }
    .executive-kpis { display:grid; grid-template-columns:repeat(6,minmax(145px,1fr)); gap:12px; margin-bottom:16px; }
    .executive-kpi { min-height:104px; padding:16px; display:flex; flex-direction:column; justify-content:space-between; gap:6px; }
    .executive-kpi span { color:#64748b; font-size:.82rem; }
    .executive-kpi strong { font-size:clamp(1.25rem,2vw,1.8rem); line-height:1.1; }
    .kpi-note { font-size:.72rem; color:#64748b; } .kpi-note.positive { color:#047857; } .kpi-note.negative { color:#b91c1c; }
    .analytics-panel { padding:16px; margin-bottom:16px; }
    .compact-filters { display:flex; flex-wrap:wrap; align-items:end; gap:10px; }
    .compact-filters label { flex:1 1 130px; max-width:190px; }
    .chart-grid { display:grid; grid-template-columns:minmax(0,1.15fr) minmax(0,1fr); gap:14px; margin-bottom:16px; }
    .chart-card { padding:18px; min-width:0; border:1px solid #cbd5e1; }
    .chart-card h3 { margin:0; font-size:1.05rem; color:#111827; }
    .chart-card p { margin:5px 0 12px; font-size:.76rem; color:#64748b; }
    .revenue-chart { display:flex; align-items:stretch; gap:8px; height:205px; overflow-x:auto; padding:16px 12px 8px; background:#f7f8fc; border:1px solid #e1e7f0; border-radius:10px; }
    .revenue-bar { min-width:38px; height:100%; flex:1; display:flex; flex-direction:column; justify-content:flex-end; text-align:center; font-size:.68rem; color:#475569; }
    .revenue-bar i { display:flex; flex:none; box-sizing:border-box; width:82%; min-height:3px; margin:4px auto 7px; border-radius:4px 4px 0 0; background:#075fd8; box-shadow:inset 0 0 0 1px rgba(4,49,119,.12); align-items:flex-start; justify-content:center; padding-top:7px; font-style:normal; overflow:visible; }
    .revenue-bar:nth-child(3n + 2) i { background:#2878df; }
    .revenue-bar:nth-child(3n) i { background:#4d8fe4; }
    .revenue-bar.zero i { background:#dbe4f3; box-shadow:none; padding-top:0; }
    .bar-value { color:#fff; font-size:.67rem; line-height:1.15; font-weight:800; white-space:nowrap; text-shadow:0 1px 2px rgba(0,0,0,.25); }
    .bar-value span, .bar-value small { display:block; }
    .bar-value small { margin-top:2px; font-size:.58rem; font-weight:700; }
    .zero-value { min-height:15px; color:#64748b; font-size:.65rem; font-weight:700; white-space:nowrap; }
    .weekly-chart { gap:10px; }
    .weekly-chart .revenue-bar { min-width:42px; }
    .weekly-chart .bar-label { font-size:.64rem; }
    .bar-label { min-height:16px; color:#475569; font-weight:600; white-space:nowrap; }
    .analytics-error { color:#b91c1c; display:flex; align-items:center; gap:12px; }
    @media (max-width:1200px) { .executive-kpis { grid-template-columns:repeat(3,1fr); } }
    @media (max-width:800px) { .chart-grid { grid-template-columns:1fr; } .revenue-chart { height:155px; } }
    @media (max-width:560px) { .executive-kpis { grid-template-columns:repeat(2,1fr); } .executive-kpi { min-height:92px; padding:12px; } .compact-filters label { max-width:none; } }
  `],
})
export class PlatformComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  readonly ui = inject(UiService);

  readonly section = signal('dashboard');
  readonly summary = signal<PlatformDashboardSummary | null>(null);
  readonly clinics = signal<AdminClinic[]>([]);
  readonly plans = signal<PlatformPlan[]>([]);
  readonly subscriptions = signal<TenantSubscription[]>([]);
  readonly reports = signal<PlatformReports | null>(null);
  readonly settings = signal<PlatformSettings | null>(null);
  readonly auditLogs = signal<Record<string, unknown>[]>([]);
  readonly editingPlanId = signal<string | null>(null);
  readonly plansLoading = signal(false);
  readonly revenue = signal<PlatformRevenueAnalytics | null>(null);
  readonly revenueLoading = signal(false);
  readonly revenueError = signal('');
  revenueFilter: { year?: number; from?: string; to?: string; tenantId?: string; planId?: string } = {};
  readonly years = Array.from({length: 7}, (_, i) => new Date().getUTCFullYear() - i);

  readonly title = computed(() => {
    const labels: Record<string, string> = {
      dashboard: 'لوحة المنصة',
      clinics: 'العيادات',
      subscriptions: 'الاشتراكات',
      plans: 'الخطط',
      reports: 'تقارير المنصة',
      'audit-logs': 'سجل العمليات',
      settings: 'إعدادات المنصة',
    };
    return labels[this.section()] ?? labels['dashboard'];
  });

  filter: Record<string, any> = { search: '', status: '', subscriptionStatus: '', page: 1, pageSize: 50 };
  planForm: Record<string, any> = {
    name: '',
    code: '',
    description: '',
    price: 0,
    currency: 'EGP',
    durationDays: 30,
    maxUsers: null,
    maxPatients: null,
    maxDoctors: null,
    featuresJson: '',
    isActive: true,
  };
  renewForm: Record<string, any> = {
    tenantId: '',
    planId: '',
    customEndDateUtc: '',
    actualPaidAmount: 0,
    paymentDateUtc: this.todayInputValue(),
    paymentMethod: '',
    notes: '',
  };
  reportFilter: Record<string, any> = { from: '', to: '', tenantId: '', planId: '' };
  settingsForm: PlatformSettings = {
    trialDurationDays: 14,
    expiringSoonThresholdDays: 7,
    defaultGracePeriodDays: 0,
    autoSuspendExpiredClinics: false,
    currencyCode: 'EGP',
    platformSupportEmail: '',
    platformSupportPhone: '',
    paymentMethodsEnabled: 'Cash,Card,Bank Transfer',
    taxPercentage: 0,
  };

  async ngOnInit() {
    this.route.url.subscribe(async (segments) => {
      const first = segments[1]?.path ?? segments[0]?.path ?? 'dashboard';
      this.section.set(first === 'platform' ? 'dashboard' : first);
      await this.load();
    });
  }

  async load() {
    const section = this.section();
    await Promise.all([
      firstValueFrom(this.api.getPlatformDashboardSummary()).then((x) => this.summary.set(x)).catch(() => this.summary.set(null)),
      section === 'clinics' || section === 'dashboard' || section === 'reports' || section === 'subscriptions'
        ? firstValueFrom(this.api.getClinics(this.filter)).then((x) => this.clinics.set(x ?? [])).catch(() => this.clinics.set([]))
        : Promise.resolve(),
      section === 'plans' || section === 'clinics' || section === 'subscriptions' || section === 'reports' || section === 'dashboard'
        ? this.loadPlans()
        : Promise.resolve(),
      section === 'dashboard' ? this.loadRevenue() : Promise.resolve(),
      section === 'subscriptions' || section === 'reports'
        ? firstValueFrom(this.api.getSubscriptions(this.filter)).then((x) => this.subscriptions.set(x ?? [])).catch(() => this.subscriptions.set([]))
        : Promise.resolve(),
      section === 'reports'
        ? this.loadReports()
        : Promise.resolve(),
      section === 'settings'
        ? this.loadSettings()
        : Promise.resolve(),
      section === 'audit-logs'
        ? firstValueFrom(this.api.getPlatformAuditLogs()).then((x) => this.auditLogs.set((x ?? []) as unknown as Record<string, unknown>[])).catch(() => this.auditLogs.set([]))
        : Promise.resolve(),
    ]);
  }

  async loadRevenue() {
    this.revenueLoading.set(true); this.revenueError.set('');
    try { this.revenue.set(await firstValueFrom(this.api.getPlatformRevenueAnalytics(this.revenueFilter))); }
    catch { this.revenue.set(null); this.revenueError.set('تعذر تحميل تحليلات الإيرادات.'); }
    finally { this.revenueLoading.set(false); }
  }
  async resetRevenueFilters() { this.revenueFilter = {}; await this.loadRevenue(); }
  barHeight(value: number, values: number[]) { const max=Math.max(...values, 0); return max ? Math.max(3, Math.round(value/max*140)) : 3; }
  seriesBarHeight(value: number, rows: PlatformMonthlyRevenue[] | PlatformWeeklyRevenue[]) { return this.barHeight(value, rows.map(row => row.revenue)); }
  average(revenue: number, count: number) { return count ? revenue/count : 0; }
  compactMoney(value: number) { return new Intl.NumberFormat('ar-EG', { notation: 'compact', maximumFractionDigits: 1 }).format(value); }

  async runExpiryCheck() {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.runSubscriptionExpiryCheck());
      await this.load();
    }, 'تم تشغيل فحص الاشتراكات');
  }

  async suspend(clinic: AdminClinic) {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.suspendClinic(clinic.id, 'Suspended from platform UI'));
      await this.load();
    }, 'تم إيقاف العيادة');
  }

  async reactivate(clinic: AdminClinic) {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.reactivateClinic(clinic.id));
      await this.load();
    }, 'تم تفعيل العيادة');
  }

  async renew(tenantId?: string) {
    const id = tenantId || this.renewForm['tenantId'];
    if (!id || !this.renewForm['planId']) {
      this.renewForm['tenantId'] = id || '';
      return;
    }
    await this.ui.run(async () => {
      const actualPaidAmount = this.resolveRenewPaidAmount();
      await firstValueFrom(this.api.renewSubscription(id, {
        planId: this.renewForm['planId'] || undefined,
        customEndDateUtc: this.toUtcDate(this.renewForm['customEndDateUtc']),
        actualPaidAmount,
        paymentDateUtc: this.toUtcDate(this.renewForm['paymentDateUtc']),
        paymentMethod: this.renewForm['paymentMethod'] || undefined,
        notes: this.renewForm['notes'] || undefined,
      }));
      this.resetRenewForm();
      await this.load();
    }, 'تم تجديد الاشتراك');
  }

  prepareRenewal(tenantId: string) {
    this.renewForm['tenantId'] = tenantId;
  }

  onRenewPlanChange() {
    const plan = this.plans().find((item) => item.id === this.renewForm['planId']);
    this.renewForm['actualPaidAmount'] = plan?.price ?? 0;
  }

  async loadReports() {
    await firstValueFrom(this.api.getPlatformReports({
      from: this.reportFilter['from'] || undefined,
      to: this.reportFilter['to'] || undefined,
      tenantId: this.reportFilter['tenantId'] || undefined,
      planId: this.reportFilter['planId'] || undefined,
    })).then((x) => this.reports.set(x)).catch(() => this.reports.set(null));
  }

  async loadSettings() {
    await firstValueFrom(this.api.getPlatformSettings()).then((x) => {
      this.settings.set(x);
      this.settingsForm = { ...x };
    }).catch(() => this.settings.set(null));
  }

  async saveSettings() {
    await this.ui.run(async () => {
      const saved = await firstValueFrom(this.api.updatePlatformSettings({
        ...this.settingsForm,
        trialDurationDays: Number(this.settingsForm.trialDurationDays || 0),
        expiringSoonThresholdDays: Number(this.settingsForm.expiringSoonThresholdDays || 0),
        defaultGracePeriodDays: Number(this.settingsForm.defaultGracePeriodDays || 0),
        taxPercentage: this.settingsForm.taxPercentage === null || this.settingsForm.taxPercentage === undefined
          ? undefined
          : Number(this.settingsForm.taxPercentage),
      }));
      this.settings.set(saved);
      this.settingsForm = { ...saved };
    }, 'تم حفظ الإعدادات');
  }

  clinicLabel(tenantId?: string) {
    if (!tenantId) return '-';
    const clinic = this.clinics().find((item) => item.id === tenantId);
    return clinic ? `${clinic.name} - ${clinic.email || clinic.subdomain}` : tenantId;
  }

  async savePlan() {
    await this.ui.run(async () => {
      const id = this.editingPlanId();
      if (id) {
        await firstValueFrom(this.api.updatePlatformPlan(id, this.planPayload()));
      } else {
        await firstValueFrom(this.api.createPlatformPlan(this.planPayload()));
      }
      this.resetPlanForm();
      await this.load();
    }, 'تم حفظ الخطة');
  }

  editPlan(plan: PlatformPlan) {
    this.editingPlanId.set(plan.id);
    this.planForm = {
      name: plan.name,
      code: plan.code,
      description: plan.description || '',
      price: plan.price,
      currency: plan.currency || 'EGP',
      durationDays: plan.durationDays,
      maxUsers: plan.maxUsers ?? null,
      maxPatients: plan.maxPatients ?? null,
      maxDoctors: plan.maxDoctors ?? null,
      featuresJson: plan.featuresJson || '',
      isActive: plan.isActive,
    };
  }

  cancelPlanEdit() {
    this.resetPlanForm();
  }

  async togglePlan(plan: PlatformPlan) {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.updatePlatformPlanStatus(plan.id, !plan.isActive));
      await this.load();
    }, 'تم تحديث الخطة');
  }

  async deletePlan(plan: PlatformPlan) {
    if (!confirm(`هل تريد حذف خطة "${plan.name}"؟`)) {
      return;
    }

    await this.ui.run(async () => {
      await firstValueFrom(this.api.deletePlatformPlan(plan.id));
      if (this.editingPlanId() === plan.id) {
        this.resetPlanForm();
      }
      await this.load();
    }, 'Plan deleted.');
  }

  statusLabel(status?: number) {
    const labels: Record<number, string> = {
      1: 'نشط',
      2: 'منتهي',
      3: 'ملغي',
      4: 'تجريبي',
      5: 'متأخر',
      6: 'موقوف',
    };
    return status ? labels[status] ?? String(status) : '-';
  }
  private resetPlanForm() {
    this.editingPlanId.set(null);
    this.planForm = {
      name: '',
      code: '',
      description: '',
      price: 0,
      currency: 'EGP',
      durationDays: 30,
      maxUsers: null,
      maxPatients: null,
      maxDoctors: null,
      featuresJson: '',
      isActive: true,
    };
  }

  private planPayload() {
    return {
      name: this.planForm['name'],
      code: this.planForm['code'],
      description: this.planForm['description'] || null,
      price: Number(this.planForm['price'] || 0),
      currency: this.planForm['currency'] || 'EGP',
      durationDays: Number(this.planForm['durationDays'] || 0),
      maxUsers: this.nullableNumber(this.planForm['maxUsers']),
      maxPatients: this.nullableNumber(this.planForm['maxPatients']),
      maxDoctors: this.nullableNumber(this.planForm['maxDoctors']),
      featuresJson: this.planForm['featuresJson'] || null,
      isActive: Boolean(this.planForm['isActive']),
    };
  }

  private nullableNumber(value: unknown) {
    return value === undefined || value === null || value === '' ? null : Number(value);
  }

  private resolveRenewPaidAmount() {
    const rawAmount = this.renewForm['actualPaidAmount'];
    const selectedPlan = this.plans().find((item) => item.id === this.renewForm['planId']);
    if (rawAmount !== undefined && rawAmount !== null && rawAmount !== '') {
      const amount = Number(rawAmount);
      return amount;
    }

    return selectedPlan?.price ?? null;
  }

  private resetRenewForm() {
    this.renewForm = {
      tenantId: '',
      planId: '',
      customEndDateUtc: '',
      actualPaidAmount: 0,
      paymentDateUtc: this.todayInputValue(),
      paymentMethod: '',
      notes: '',
    };
  }

  private toUtcDate(value: string | undefined | null) {
    return value ? `${value}T00:00:00Z` : undefined;
  }

  private todayInputValue() {
    return new Date().toISOString().slice(0, 10);
  }

  private async loadPlans() {
    this.plansLoading.set(true);
    try {
      const plans = await firstValueFrom(this.api.getPlatformPlans());
      this.plans.set(plans ?? []);
    } catch (error) {
      this.plans.set([]);
      this.ui.error.set(this.readHttpError(error));
    } finally {
      this.plansLoading.set(false);
    }
  }

  private readHttpError(error: any) {
    const body = error?.error;
    return String(body?.detail || body?.message || body?.title || error?.message || 'تعذر تحميل الخطط.').trim();
  }
}
