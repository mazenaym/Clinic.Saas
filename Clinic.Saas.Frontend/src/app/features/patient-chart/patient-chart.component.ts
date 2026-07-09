import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import {
  Patient,
  PatientChart,
  PatientDocument,
  PatientFinancialLedger,
  PatientTimelineItem,
} from '../../core/models';
import { UiService } from '../../core/ui.service';

type ChartTab = 'overview' | 'visits' | 'prescriptions' | 'appointments' | 'ledger' | 'documents' | 'invoices';

@Component({
  selector: 'app-patient-chart',
  imports: [DatePipe, DecimalPipe, FormsModule, RouterLink],
  templateUrl: './patient-chart.component.html',
})
export class PatientChartComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  readonly ui = inject(UiService);

  readonly patient = signal<Patient | null>(null);
  readonly chart = signal<PatientChart | null>(null);
  readonly timeline = signal<PatientTimelineItem[]>([]);
  readonly ledger = signal<PatientFinancialLedger | null>(null);
  readonly documents = signal<PatientDocument[]>([]);
  readonly activeTab = signal<ChartTab>('overview');
  readonly loading = signal(true);
  readonly error = signal('');
  readonly clinicalError = signal('');
  readonly billingError = signal('');
  readonly documentsError = signal('');
  readonly documentsLoading = signal(false);
  readonly uploading = signal(false);
  documentUploadForm = { documentType: 1, description: '' };
  readonly documentTypes = [
    { value: 1, label: 'نتيجة معمل' },
    { value: 2, label: 'أشعة' },
    { value: 3, label: 'إحالة' },
    { value: 4, label: 'تأمين' },
    { value: 5, label: 'موافقة' },
    { value: 6, label: 'أخرى' },
  ];

  readonly canSeeClinical = computed(() => this.auth.hasRole(['Admin', 'Doctor']));
  readonly canSeeBilling = computed(() => this.auth.hasRole(['Admin', 'Reception']));
  readonly canUploadDocuments = computed(() => this.auth.hasRole(['Admin', 'Doctor', 'Reception']));
  readonly invoiceTransactions = computed(() => this.ledger()?.entries.filter((entry) => entry.type.toLowerCase().includes('invoice')) ?? []);
  readonly paymentTransactions = computed(() => this.ledger()?.entries.filter((entry) => entry.type.toLowerCase().includes('payment')) ?? []);

  readonly visibleTabs = computed(() => {
    const tabs: { key: ChartTab; label: string }[] = [
      { key: 'overview', label: 'ملخص' },
      { key: 'documents', label: 'المستندات' },
    ];
    if (this.canSeeClinical()) {
      tabs.splice(1, 0, { key: 'visits', label: 'الزيارات' }, { key: 'prescriptions', label: 'الروشتات' }, { key: 'appointments', label: 'المواعيد' });
    }
    if (this.canSeeBilling()) {
      tabs.push({ key: 'ledger', label: 'الحسابات' }, { key: 'invoices', label: 'الفواتير' });
    }
    return tabs;
  });

  async ngOnInit() {
    const requestedTab = this.route.snapshot.queryParamMap.get('tab') as ChartTab | null;
    if (requestedTab) this.activeTab.set(requestedTab);
    await this.load();
  }

  async load() {
    const patientId = this.route.snapshot.paramMap.get('id');
    if (!patientId) {
      this.error.set('رقم المريض غير موجود.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.clinicalError.set('');
    this.billingError.set('');
    this.documentsError.set('');

    try {
      const patient = await firstValueFrom(this.api.patient(patientId));
      this.patient.set(patient);
    } catch {
      this.error.set('تعذر تحميل بيانات المريض.');
      this.loading.set(false);
      return;
    }

    await Promise.all([
      this.loadClinical(patientId),
      this.loadBilling(patientId),
      this.loadDocuments(patientId),
    ]);

    this.ensureVisibleTab();
    this.loading.set(false);
  }

  async uploadDocument(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const patientId = this.patient()?.id;
    if (!file || !patientId) return;

    this.uploading.set(true);
    this.documentsError.set('');
    try {
      await firstValueFrom(this.api.uploadPatientDocument(patientId, file, this.documentUploadForm));
      await this.loadDocuments(patientId);
      this.documentUploadForm = { documentType: 1, description: '' };
      input.value = '';
    } catch {
      this.documentsError.set('تعذر رفع المستند.');
    } finally {
      this.uploading.set(false);
    }
  }

  async openDocument(document: PatientDocument) {
    const patientId = this.patient()?.id;
    if (!patientId) return;
    const tab = window.open('', '_blank');
    try {
      await this.ui.run(async () => {
        const file = await firstValueFrom(this.api.viewPatientDocument(patientId, document.id));
        const url = URL.createObjectURL(file);
        if (tab) {
          tab.location.href = url;
          window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
        } else {
          this.downloadBlob(file, document.fileName);
        }
      }, 'تم فتح المستند');
    } catch {
      tab?.close();
    }
  }

  async downloadDocument(document: PatientDocument) {
    const patientId = this.patient()?.id;
    if (!patientId) return;
    await this.ui.run(async () => {
      const file = await firstValueFrom(this.api.downloadPatientDocument(patientId, document.id));
      this.downloadBlob(file, document.fileName);
    }, 'تم تنزيل المستند');
  }

  documentTypeLabel(documentType: number) {
    return this.documentTypes.find((type) => type.value === Number(documentType))?.label ?? 'أخرى';
  }

  documentKind(document: PatientDocument) {
    const type = document.fileType?.toLowerCase() || '';
    const name = document.fileName?.toLowerCase() || '';
    if (type.includes('pdf') || name.endsWith('.pdf')) return 'PDF';
    if (type.startsWith('image/') || /\.(png|jpe?g|gif|webp|bmp)$/i.test(name)) return 'Image';
    if (type.includes('word') || /\.(docx?|rtf)$/i.test(name)) return 'Word';
    return type || 'File';
  }

  uploadedBy(document: PatientDocument) {
    return document.uploadedBy || 'النظام';
  }

  private async loadClinical(patientId: string) {
    if (!this.canSeeClinical()) return;
    try {
      const [chart, timeline] = await Promise.all([
        firstValueFrom(this.api.getPatientChart(patientId)).catch(() => null),
        firstValueFrom(this.api.getPatientTimeline(patientId)).catch(() => []),
      ]);
      this.chart.set(chart);
      this.timeline.set(timeline.length ? timeline : chart?.timeline ?? []);
    } catch {
      this.clinicalError.set('تعذر تحميل بيانات الملف الطبي.');
    }
  }

  private async loadBilling(patientId: string) {
    if (!this.canSeeBilling()) return;
    try {
      this.ledger.set(await firstValueFrom(this.api.getPatientLedger(patientId)));
    } catch {
      this.billingError.set('تعذر تحميل بيانات الحسابات.');
    }
  }

  private async loadDocuments(patientId: string) {
    if (!this.canUploadDocuments()) return;
    this.documentsLoading.set(true);
    this.documentsError.set('');
    try {
      this.documents.set(await firstValueFrom(this.api.listPatientDocuments(patientId)));
    } catch {
      this.documentsError.set('تعذر تحميل المستندات.');
    } finally {
      this.documentsLoading.set(false);
    }
  }

  private ensureVisibleTab() {
    const visible = this.visibleTabs().some((tab) => tab.key === this.activeTab());
    if (!visible) this.activeTab.set('overview');
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
