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
  readonly monthly = signal<Record<string, unknown>[]>([]);
  readonly debts = signal<Record<string, unknown>[]>([]);
  readonly selectedPayment = signal<Record<string, unknown> | null>(null);
  readonly patientPayments = signal<Record<string, unknown>[]>([]);
  readonly editingPaymentId = signal('');
  lookupPaymentId = '';
  readonly methods = enumValues.paymentMethod;
  readonly serviceTypes = enumValues.serviceType;
  date = new Date().toISOString().slice(0, 10);
  form: Record<string, any> = this.emptyForm();

  async ngOnInit() {
    this.patients.set(await firstValueFrom(this.api.patients()).catch(() => []));
    await this.loadReport();
    await this.loadExtras();
  }

  emptyForm() {
    return {
      visitId: '', patientId: '', totalAmount: 0, discountAmount: 0, discountPct: 0, taxAmount: 0, paidAmount: 0,
      paymentMethod: 1, insuranceCompany: '', insuranceNumber: '', notes: '',
      items: [{ serviceName: 'كشف', serviceType: 1, quantity: 1, unitPrice: 0, discountPct: 0 }],
    };
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
      const id = this.editingPaymentId();
      if (id) {
        await firstValueFrom(this.api.updatePayment(id, this.form));
      } else {
        await firstValueFrom(this.api.createPayment(this.form));
      }
      this.cancelEdit();
      await this.loadReport();
      await this.loadExtras();
    }, this.editingPaymentId() ? 'تم تحديث الدفع' : 'تم تسجيل الدفع');
  }

  async loadPayment() {
    if (!this.lookupPaymentId) return;
    const data = await firstValueFrom(this.api.payment(this.lookupPaymentId));
    this.selectedPayment.set(data);
    const payment = (data['payment'] || {}) as Record<string, any>;
    this.form = {
      ...this.emptyForm(),
      ...payment,
      paymentMethod: Number(payment['paymentMethod'] ?? 1),
      items: (data['items'] as any[])?.map((item) => ({
        serviceName: item.serviceName,
        serviceType: Number(item.serviceType),
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        discountPct: item.discountPct,
      })) || this.emptyForm()['items'],
    };
    this.editingPaymentId.set(this.lookupPaymentId);
  }

  async loadPatientPayments() {
    const patientId = this.form['patientId'];
    if (!patientId) return;
    this.patientPayments.set(await firstValueFrom(this.api.patientPayments(patientId)).catch(() => []));
  }

  cancelEdit() {
    this.editingPaymentId.set('');
    this.selectedPayment.set(null);
    this.lookupPaymentId = '';
    this.form = this.emptyForm();
  }

  async loadReport() {
    this.report.set(await firstValueFrom(this.api.dailyRevenue(this.date)).catch(() => null));
  }

  async loadExtras() {
    const d = new Date(this.date);
    this.monthly.set(await firstValueFrom(this.api.monthlyRevenue(d.getFullYear(), d.getMonth() + 1)).catch(() => []));
    this.debts.set(await firstValueFrom(this.api.debts()).catch(() => []));
  }

  receiptUrl(id: string) { return this.api.receiptPdfUrl(id); }

  async refund(id: string) {
    const reason = prompt('Refund reason') || '';
    await this.ui.run(async () => {
      await firstValueFrom(this.api.refundPayment(id, reason));
      await this.loadExtras();
      await this.loadReport();
    }, 'تم رد الدفع');
  }
}
