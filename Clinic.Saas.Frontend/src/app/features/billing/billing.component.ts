import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { DailyRevenue, Patient, enumValues } from '../../core/models';
import { UiService } from '../../core/ui.service';

@Component({
  selector: 'app-billing',
  imports: [FormsModule, DecimalPipe, RouterLink],
  templateUrl: './billing.component.html',
})
export class BillingComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly ui = inject(UiService);
  readonly patients = signal<Patient[]>([]);
  readonly report = signal<DailyRevenue | null>(null);
  readonly methods = enumValues.paymentMethod;
  readonly serviceTypes = enumValues.serviceType;
  date = new Date().toISOString().slice(0, 10);
  form: Record<string, any> = {
    visitId: '', patientId: '', totalAmount: 0, discountAmount: 0, discountPct: 0, taxAmount: 0, paidAmount: 0,
    paymentMethod: 1, insuranceCompany: '', insuranceNumber: '', notes: '',
    items: [{ serviceName: 'كشف', serviceType: 1, quantity: 1, unitPrice: 0, discountPct: 0 }],
  };

  async ngOnInit() {
    this.patients.set(await firstValueFrom(this.api.patients()).catch(() => []));
    await this.loadReport();
  }

  items() { return this.form['items'] as any[]; }
  addItem() { this.items().push({ serviceName: '', serviceType: 1, quantity: 1, unitPrice: 0, discountPct: 0 }); }
  removeItem(index: number) { this.items().splice(index, 1); this.recalculate(); }
  recalculate() {
    const total = this.items().reduce((sum, item) => sum + Number(item.quantity || 0) * Number(item.unitPrice || 0), 0);
    this.form['totalAmount'] = total;
    this.form['paidAmount'] = Math.max(0, total - Number(this.form['discountAmount'] || 0) + Number(this.form['taxAmount'] || 0));
  }

  async create() {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.createPayment(this.form));
      await this.loadReport();
    }, 'تم تسجيل الدفع');
  }

  async loadReport() {
    this.report.set(await firstValueFrom(this.api.dailyRevenue(this.date)).catch(() => null));
  }
}
