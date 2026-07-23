import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AddInvoicePaymentPayload, CreateInvoicePayload, Invoice, Patient, PatientFinancialLedgerEntry, enumValues } from '../../core/models';
import { UiService } from '../../core/ui.service';

@Component({
  selector: 'app-invoices',
  imports: [DecimalPipe, FormsModule, RouterLink],
  templateUrl: './invoices.component.html',
})
export class InvoicesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly ui = inject(UiService);

  readonly patients = signal<Patient[]>([]);
  readonly invoice = signal<Invoice | null>(null);
  readonly patientLedgerEntries = signal<PatientFinancialLedgerEntry[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly methods = enumValues.paymentMethod;
  readonly serviceTypes = enumValues.serviceType;
  lookupInvoiceId = '';
  invoiceForm: CreateInvoicePayload = this.emptyInvoiceForm();
  paymentForm: AddInvoicePaymentPayload = { amount: 0, paymentMethod: 1, notes: '' };

  readonly totals = computed(() => {
    const subtotal = this.invoiceForm.items.reduce((sum, item) => sum + Number(item.quantity || 0) * Number(item.unitPrice || 0), 0);
    const discount = this.invoiceForm.items.reduce((sum, item) => sum + Number(item.discountAmount || 0), 0);
    const tax = this.invoiceForm.items.reduce((sum, item) => sum + Number(item.taxAmount || 0), 0);
    const grandTotal = Math.max(0, subtotal - discount + tax);
    const paid = this.invoice()?.paidAmount ?? 0;
    return { subtotal, discount, tax, grandTotal, paid, remaining: Math.max(0, grandTotal - paid) };
  });

  async ngOnInit() {
    this.patients.set(await firstValueFrom(this.api.patients()).catch(() => []));
    const patientId = this.route.snapshot.queryParamMap.get('patientId') || this.patients()[0]?.id || '';
    this.invoiceForm = this.emptyInvoiceForm(patientId);
    if (patientId) await this.loadPatientLedger(patientId);

    const invoiceId = this.route.snapshot.paramMap.get('id');
    if (invoiceId) {
      this.lookupInvoiceId = invoiceId;
      await this.loadInvoice(invoiceId);
    }
  }

  emptyInvoiceForm(patientId = ''): CreateInvoicePayload {
    return {
      patientId,
      visitId: undefined,
      notes: '',
      items: [{ description: 'Consultation', serviceType: 1, quantity: 1, unitPrice: 0, discountAmount: 0, taxAmount: 0 }],
    };
  }

  addItem() {
    this.invoiceForm.items.push({ description: '', serviceType: 1, quantity: 1, unitPrice: 0, discountAmount: 0, taxAmount: 0 });
  }

  removeItem(index: number) {
    if (this.invoiceForm.items.length === 1) return;
    this.invoiceForm.items.splice(index, 1);
  }

  async patientChanged() {
    this.invoice.set(null);
    await this.loadPatientLedger(this.invoiceForm.patientId);
  }

  async createInvoice() {
    await this.ui.run(async () => {
      const payload: CreateInvoicePayload = {
        ...this.invoiceForm,
        visitId: this.invoiceForm.visitId || undefined,
        items: this.invoiceForm.items.map((item) => ({
          ...item,
          quantity: Number(item.quantity || 0),
          unitPrice: Number(item.unitPrice || 0),
          discountAmount: Number(item.discountAmount || 0),
          taxAmount: Number(item.taxAmount || 0),
        })),
      };
      const created = await firstValueFrom(this.api.createInvoice(payload));
      this.invoice.set(created);
      this.paymentForm.amount = created.remainingAmount;
      await this.loadPatientLedger(payload.patientId);
      await this.router.navigate(['/invoices', created.id], { replaceUrl: true });
    }, 'تم إنشاء الفاتورة');
  }

  async loadInvoice(id = this.lookupInvoiceId) {
    if (!id) return;
    this.loading.set(true);
    this.error.set('');
    try {
      const invoice = await firstValueFrom(this.api.getInvoiceById(id));
      this.invoice.set(invoice);
      this.lookupInvoiceId = invoice.id;
      this.invoiceForm = {
        patientId: invoice.patientId,
        visitId: invoice.visitId,
        notes: invoice.notes,
        items: invoice.items.map((item) => ({
          procedureId: item.procedureId,
          description: item.description,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          discountAmount: item.discountAmount,
          taxAmount: item.taxAmount,
        })),
      };
      this.paymentForm.amount = invoice.remainingAmount;
      await this.loadPatientLedger(invoice.patientId);
    } catch {
      this.error.set('تعذر تحميل تفاصيل الفاتورة.');
    } finally {
      this.loading.set(false);
    }
  }

  async addPayment() {
    const invoice = this.invoice();
    if (!invoice) return;
    await this.ui.run(async () => {
      const updated = await firstValueFrom(this.api.addInvoicePayment(invoice.id, {
        ...this.paymentForm,
        amount: Number(this.paymentForm.amount || 0),
        paymentMethod: Number(this.paymentForm.paymentMethod || 1),
      }));
      this.invoice.set(updated);
      this.paymentForm = { amount: updated.remainingAmount, paymentMethod: 1, notes: '' };
      await this.loadPatientLedger(updated.patientId);
    }, 'تم تسجيل الدفعة');
  }

  async downloadPdf() {
    const invoice = this.invoice(); if (!invoice) return;
    await this.ui.run(async () => {
      const blob = await firstValueFrom(this.api.invoicePdf(invoice.id));
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a'); anchor.href = url; anchor.download = `invoice-${invoice.invoiceNumber}.pdf`;
      anchor.click(); URL.revokeObjectURL(url);
    }, 'تم تنزيل الفاتورة PDF');
  }

  invoiceStatus(invoice: Invoice) {
    if (invoice.status) return invoice.status;
    if (invoice.remainingAmount <= 0) return 'Paid';
    if (invoice.paidAmount > 0) return 'Partial';
    return 'Unpaid';
  }

  private async loadPatientLedger(patientId: string) {
    if (!patientId) {
      this.patientLedgerEntries.set([]);
      return;
    }
    const ledger = await firstValueFrom(this.api.getPatientLedger(patientId)).catch(() => null);
    this.patientLedgerEntries.set(ledger?.entries ?? []);
  }
}
