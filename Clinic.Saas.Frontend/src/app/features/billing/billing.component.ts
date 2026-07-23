import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { DailyRevenue, Invoice, Patient, Visit, enumValues } from '../../core/models';
import { UiService } from '../../core/ui.service';
import { CfConfirmDialogService } from '../../shared/ui';

@Component({
  selector: 'app-billing',
  imports: [FormsModule, DecimalPipe, DatePipe],
  templateUrl: './billing.component.html',
})
export class BillingComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(CfConfirmDialogService);
  readonly ui = inject(UiService);
  readonly patients = signal<Patient[]>([]);
  readonly visits = signal<Visit[]>([]);
  readonly report = signal<DailyRevenue | null>(null);
  readonly monthly = signal<Record<string, unknown>[]>([]);
  readonly debts = signal<Record<string, unknown>[]>([]);
  readonly selectedInvoice = signal<Invoice | null>(null);
  readonly patientInvoices = signal<Invoice[]>([]);
  readonly editingInvoiceId = signal('');
  lookupInvoiceId = '';
  encounterSearch = '';
  readonly methods = enumValues.paymentMethod;
  readonly serviceTypes = enumValues.serviceType;
  date = new Date().toISOString().slice(0, 10);
  form: Record<string, any> = this.emptyForm();

  async ngOnInit() {
    this.patients.set(await firstValueFrom(this.api.patients()).catch(() => []));
    this.applyDefaultLookups();
    await this.loadEncounters();
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
  private applyDefaultLookups() {
    this.form['patientId'] ||= this.patients()[0]?.id || '';
  }
  async patientChanged() {
    this.form['visitId'] = '';
    this.encounterSearch = '';
    await Promise.all([this.loadEncounters(), this.loadPatientInvoices()]);
  }
  async loadEncounters() {
    const patientId = this.form['patientId'];
    this.visits.set(patientId ? await firstValueFrom(this.api.visitHistory(patientId)).catch(() => []) : []);
    this.form['visitId'] = this.visits()[0]?.id || '';
  }
  filteredVisits() {
    const query = this.encounterSearch.trim().toLocaleLowerCase('ar');
    if (!query) return this.visits();
    return this.visits().filter((visit) =>
      [visit.visitDate, visit.doctorName, visit.chiefComplaint, visit.diagnosis]
        .some((value) => String(value || '').toLocaleLowerCase('ar').includes(query)));
  }
  addItem() { this.items().push({ serviceName: '', serviceType: 1, quantity: 1, unitPrice: 0, discountPct: 0 }); }
  removeItem(index: number) { this.items().splice(index, 1); this.recalculate(); }
  recalculate() {
    const total = this.items().reduce((sum, item) => sum + Number(item.quantity || 0) * Number(item.unitPrice || 0), 0);
    this.form['totalAmount'] = total;
  }

  async create() {
    await this.ui.run(async () => {
      this.applyDefaultLookups();
      this.recalculate();
      const id = this.editingInvoiceId();
      if (id) {
        await firstValueFrom(this.api.updateInvoice(id, { ...this.form, rowVersion: this.form['rowVersion'] || this.invoiceRowVersion(id) }));
      } else {
        const payload = {
          patientId: this.form['patientId'],
          visitId: this.form['visitId'],
          notes: this.form['notes'],
          items: this.items().map((item: any) => ({
            description: item.serviceName,
            serviceType: item.serviceType,
            quantity: item.quantity,
            unitPrice: item.unitPrice,
            discountAmount: item.discountAmount || 0,
            taxAmount: item.taxAmount || 0,
          })),
        };
        await firstValueFrom(this.api.createInvoice(payload));
      }
      this.cancelEdit();
      await this.loadReport();
      await this.loadExtras();
    }, this.editingInvoiceId() ? 'تم تحديث الفاتورة' : 'تم تسجيل الفاتورة');
  }

  async loadInvoice() {
    if (!this.lookupInvoiceId) return;
    const data = await firstValueFrom(this.api.getInvoiceById(this.lookupInvoiceId));
    this.selectedInvoice.set(data);
    this.form = {
      ...this.emptyForm(),
      patientId: data.patientId,
      visitId: data.visitId || '',
      notes: data.notes || '',
      rowVersion: data.rowVersion,
      items: data.items?.map((item) => ({
        serviceName: item.description,
        serviceType: item.serviceType || 1,
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        discountPct: item.discountAmount || 0,
      })) || this.emptyForm()['items'],
    };
    this.editingInvoiceId.set(this.lookupInvoiceId);
  }

  async loadPatientInvoices() {
    const patientId = this.form['patientId'];
    if (!patientId) return;
    this.patientInvoices.set(await firstValueFrom(this.api.patientInvoices(patientId)).catch(() => []));
  }

  cancelEdit() {
    this.editingInvoiceId.set('');
    this.selectedInvoice.set(null);
    this.lookupInvoiceId = '';
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

  async downloadReceipt(id: string) {
    await this.ui.run(async () => {
      const receipt = await firstValueFrom(this.api.receiptPdf(id));
      this.downloadBlob(receipt, `receipt-${id}.pdf`);
    }, 'تم تنزيل الإيصال');
  }

  async refund(id: string) {
    const reason = await this.dialog.prompt({
      title: 'رد فاتورة',
      message: 'اكتب سبب رد الفاتورة قبل المتابعة.',
      inputLabel: 'سبب الرد',
      confirmLabel: 'رد الفاتورة',
    });
    if (!reason) return;
    await this.ui.run(async () => {
      await firstValueFrom(this.api.refundInvoice(id, reason, this.invoiceRowVersion(id)));
      await this.loadExtras();
      await this.loadReport();
    }, 'تم رد الفاتورة');
  }

  private invoiceRowVersion(id: string) {
    const selected = this.selectedInvoice();
    if (selected?.id === id) return selected.rowVersion;
    return this.patientInvoices().find((inv) => inv.id === id)?.rowVersion;
  }

  private downloadBlob(blob: Blob, filename: string) {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  }
}
