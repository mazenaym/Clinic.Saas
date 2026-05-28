import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Patient, PatientTimelineItem, enumValues } from '../../core/models';
import { UiService } from '../../core/ui.service';

@Component({
  selector: 'app-patients',
  imports: [FormsModule],
  templateUrl: './patients.component.html',
})
export class PatientsComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly ui = inject(UiService);
  readonly patients = signal<Patient[]>([]);
  readonly selected = signal<Patient | null>(null);
  readonly timeline = signal<PatientTimelineItem[]>([]);
  readonly duplicates = signal<Patient[]>([]);
  readonly showForm = signal(false);
  readonly showMore = signal(false);
  readonly genders = enumValues.gender;
  search = '';
  form: Record<string, any> = { fullName: '', phoneNumber: '', nationalId: '', gender: 1, bloodType: '', dateOfBirth: '', email: '', address: '', medicalHistory: '', drugAllergies: '', chronicDiseases: '' };

  ngOnInit() { this.load(); }

  async load() {
    this.patients.set(await firstValueFrom(this.api.patients(this.search)));
  }

  edit(patient: Patient) {
    this.selected.set(patient);
    this.showForm.set(true);
    this.showMore.set(true);
    this.form = { ...patient, gender: patient.gender === 'Female' || patient.gender === 'أنثى' ? 2 : 1 };
    this.loadTimeline(patient.id);
  }

  reset() {
    this.selected.set(null);
    this.timeline.set([]);
    this.duplicates.set([]);
    this.showForm.set(false);
    this.showMore.set(false);
    this.form = { fullName: '', phoneNumber: '', nationalId: '', gender: 1, bloodType: '', dateOfBirth: '', email: '', address: '', medicalHistory: '', drugAllergies: '', chronicDiseases: '' };
  }

  async save() {
    await this.ui.run(async () => {
      const selected = this.selected();
      const id = selected?.id;
      if (id) await firstValueFrom(this.api.updatePatient(id, { ...this.form, rowVersion: selected?.rowVersion || this.form['rowVersion'] }));
      else await firstValueFrom(this.api.createPatient(this.form));
      this.reset();
      await this.load();
    }, 'تم حفظ بيانات المريض');
  }

  async remove(id: string) {
    await this.ui.run(async () => {
      const rowVersion = this.patients().find((patient) => patient.id === id)?.rowVersion;
      await firstValueFrom(this.api.deletePatient(id, rowVersion));
      await this.load();
    }, 'تم حذف المريض');
  }

  async loadTimeline(id = this.selected()?.id) {
    if (!id) return;
    this.timeline.set(await firstValueFrom(this.api.patientTimeline(id)).catch(() => []));
  }

  async checkDuplicates() {
    this.duplicates.set(await firstValueFrom(this.api.patientDuplicates(this.form['phoneNumber'], this.form['nationalId'])).catch(() => []));
  }

  exportPatients() {
    window.open(this.api.exportPatientsUrl(), '_blank');
  }

  async uploadDocument(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const patientId = this.selected()?.id;
    if (!file || !patientId) return;
    await this.ui.run(async () => {
      await firstValueFrom(this.api.uploadPatientDocument(patientId, file));
      input.value = '';
    }, 'تم رفع مستند المريض');
  }

  async importPatients(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const text = await file.text();
    const [headerLine, ...lines] = text.split(/\r?\n/).filter((line) => line.trim());
    const headers = headerLine.split(',').map((x) => x.trim());
    const rows = lines.map((line) => {
      const values = line.split(',').map((x) => x.trim());
      return headers.reduce<Record<string, string>>((row, key, index) => {
        row[key] = values[index] ?? '';
        return row;
      }, {});
    });

    await this.ui.run(async () => {
      for (const row of rows) {
        await firstValueFrom(this.api.createPatient({
          fullName: row['fullName'] || row['name'] || row['الاسم'],
          phoneNumber: row['phoneNumber'] || row['phone'] || row['الهاتف'],
          nationalId: row['nationalId'] || row['nationalIdNumber'] || row['الرقم القومي'] || '',
          gender: Number(row['gender'] || 1),
          dateOfBirth: row['dateOfBirth'] || '',
          email: row['email'] || '',
          address: row['address'] || '',
        }));
      }
      input.value = '';
      await this.load();
    }, `تم استيراد ${rows.length} مريض`);
  }
}
