import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Patient, Prescription, PrescriptionItem, User } from '../../core/models';
import { UiService } from '../../core/ui.service';

@Component({
  selector: 'app-prescriptions',
  imports: [FormsModule, RouterLink],
  templateUrl: './prescriptions.component.html',
})
export class PrescriptionsComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly ui = inject(UiService);
  readonly patients = signal<Patient[]>([]);
  readonly users = signal<User[]>([]);
  readonly result = signal<Prescription | null>(null);
  readonly drugs = signal<Record<string, unknown>[]>([]);
  readonly interactionWarnings = signal<Record<string, unknown>[]>([]);
  readonly templates = signal<Record<string, unknown>[]>([]);
  drugTerm = '';
  lookupId = '';
  form: Record<string, any> = { visitId: '', patientId: '', doctorId: '', notes: '', items: [{ drugName: '', dosage: '', frequency: '', duration: '', route: '', instructions: '' }] };
  templateForm: Record<string, any> = { name: '', specialty: '', chiefComplaint: '', clinicalNotes: '', diagnosis: '' };

  async ngOnInit() {
    this.patients.set(await firstValueFrom(this.api.patients()).catch(() => []));
    this.users.set(await firstValueFrom(this.api.users()).catch(() => []));
    this.templates.set(await firstValueFrom(this.api.clinicalTemplates()).catch(() => []));
  }

  doctors() { return this.users().filter((u) => u.role === 'Doctor'); }
  items() { return this.form['items'] as PrescriptionItem[]; }
  addItem() { this.items().push({ drugName: '', dosage: '', frequency: '', duration: '', route: '', instructions: '' }); }
  removeItem(index: number) { this.items().splice(index, 1); }
  async searchDrugs() { this.drugs.set(await firstValueFrom(this.api.drugs(this.drugTerm)).catch(() => [])); }
  useDrug(name: unknown, index = 0) { if (this.items()[index]) this.items()[index].drugName = String(name ?? ''); }
  async checkInteractions() {
    const names = this.items().map((x) => x.drugName).filter(Boolean);
    this.interactionWarnings.set(await firstValueFrom(this.api.checkDrugInteractions(names)).catch(() => []));
  }

  async createTemplate() {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.createClinicalTemplate(this.templateForm));
      this.templateForm = { name: '', specialty: '', chiefComplaint: '', clinicalNotes: '', diagnosis: '' };
      this.templates.set(await firstValueFrom(this.api.clinicalTemplates()).catch(() => []));
    }, 'تم حفظ القالب');
  }

  async create() {
    await this.ui.run(async () => {
      this.result.set(await firstValueFrom(this.api.createPrescription(this.form)));
    }, 'تم إنشاء الروشتة');
  }

  async loadPrescription() {
    this.result.set(await firstValueFrom(this.api.prescription(this.lookupId)));
  }
  pdfUrl() { return this.result() ? this.api.prescriptionPdfUrl(this.result()!.id) : '#'; }
  async sendWhatsapp() {
    const id = this.result()?.id;
    if (!id) return;
    await this.ui.run(async () => {
      await firstValueFrom(this.api.sendPrescriptionWhatsapp(id));
      await this.loadPrescription();
    }, 'تم إرسال الروشتة واتساب');
  }
}
