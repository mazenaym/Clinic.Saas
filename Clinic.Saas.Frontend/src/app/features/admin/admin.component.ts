import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AdminClinic, AdminStats, enumValues } from '../../core/models';
import { UiService } from '../../core/ui.service';

@Component({
  selector: 'app-admin',
  imports: [FormsModule, DecimalPipe],
  templateUrl: './admin.component.html',
})
export class AdminComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly ui = inject(UiService);
  readonly stats = signal<AdminStats | null>(null);
  readonly clinics = signal<AdminClinic[]>([]);
  readonly plans = enumValues.plans;
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
