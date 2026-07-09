import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AdminClinic, ClinicSubscription, enumValues } from '../../core/models';
import { UiService } from '../../core/ui.service';

@Component({
  selector: 'app-admin-clinic-detail',
  imports: [DatePipe, DecimalPipe, FormsModule, RouterLink],
  templateUrl: './admin-clinic-detail.component.html',
})
export class AdminClinicDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  readonly ui = inject(UiService);

  readonly clinic = signal<AdminClinic | null>(null);
  readonly subscriptions = signal<ClinicSubscription[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly plans = enumValues.plans;
  readonly subscriptionStatuses = [
    { value: 1, label: 'Active' },
    { value: 2, label: 'Expired' },
    { value: 3, label: 'Cancelled' },
    { value: 4, label: 'Trial' },
  ];

  form: Record<string, any> = {};
  subscriptionForm = {
    plan: 1,
    startDate: new Date().toISOString().slice(0, 10),
    endDate: new Date(new Date().setMonth(new Date().getMonth() + 1)).toISOString().slice(0, 10),
    amountPaid: 0,
    status: 1,
    paymentRef: '',
    notes: '',
  };

  async ngOnInit() {
    await this.load();
  }

  async load() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Clinic id is missing.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set('');
    try {
      const [clinic, subscriptions] = await Promise.all([
        firstValueFrom(this.api.getClinicDetails(id)),
        firstValueFrom(this.api.getClinicSubscriptions(id)).catch(() => []),
      ]);
      this.clinic.set(clinic);
      this.subscriptions.set(subscriptions);
      this.form = {
        name: clinic.name,
        subdomain: clinic.subdomain,
        email: clinic.email,
        phone: clinic.phone || '',
        logoUrl: clinic.logoUrl || '',
        plan: clinic.plan,
        timeZone: clinic.timeZone || 'Africa/Cairo',
        currency: clinic.currency || 'EGP',
        isActive: clinic.isActive,
      };
      this.subscriptionForm.plan = clinic.plan || 1;
    } catch {
      this.error.set('Unable to load clinic details.');
    } finally {
      this.loading.set(false);
    }
  }

  async save() {
    const id = this.clinic()?.id;
    if (!id) return;
    await this.ui.run(async () => {
      const updated = await firstValueFrom(this.api.updateClinic(id, this.form));
      this.clinic.set(updated);
      await this.load();
    }, 'Clinic updated');
  }

  async toggleStatus() {
    const clinic = this.clinic();
    if (!clinic) return;
    await this.ui.run(async () => {
      const updated = await firstValueFrom(this.api.setClinicStatus(clinic.id, !clinic.isActive));
      this.clinic.set(updated);
      await this.load();
    }, 'Clinic status updated');
  }

  async addSubscription() {
    const id = this.clinic()?.id;
    if (!id) return;
    await this.ui.run(async () => {
      await firstValueFrom(this.api.addClinicSubscription(id, {
        ...this.subscriptionForm,
        plan: Number(this.subscriptionForm.plan),
        amountPaid: Number(this.subscriptionForm.amountPaid || 0),
        status: Number(this.subscriptionForm.status),
        paymentRef: this.subscriptionForm.paymentRef || undefined,
        notes: this.subscriptionForm.notes || undefined,
      }));
      await this.load();
    }, 'Subscription added');
  }

  planLabel(plan?: number) {
    return this.plans.find((item) => item.value === Number(plan))?.label ?? String(plan ?? '-');
  }

  statusLabel(status?: number) {
    return this.subscriptionStatuses.find((item) => item.value === Number(status))?.label ?? String(status ?? '-');
  }
}
