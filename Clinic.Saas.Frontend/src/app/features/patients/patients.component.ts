import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Patient, enumValues } from '../../core/models';
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
  readonly showForm = signal(false);
  readonly showMore = signal(false);
  readonly genders = enumValues.gender;
  search = '';
  form: Record<string, any> = { fullName: '', phoneNumber: '', gender: 1, bloodType: '', dateOfBirth: '', email: '', address: '', medicalHistory: '', drugAllergies: '', chronicDiseases: '' };

  ngOnInit() {
    this.load();
  }

  async load() {
    this.patients.set(await firstValueFrom(this.api.patients(this.search)));
  }

  edit(patient: Patient) {
    this.selected.set(patient);
    this.showForm.set(true);
    this.showMore.set(true);
    this.form = { ...patient, gender: patient.gender === 'Female' || patient.gender === 'أنثى' ? 2 : 1 };
  }

  reset() {
    this.selected.set(null);
    this.showForm.set(false);
    this.showMore.set(false);
    this.form = { fullName: '', phoneNumber: '', gender: 1, bloodType: '', dateOfBirth: '', email: '', address: '', medicalHistory: '', drugAllergies: '', chronicDiseases: '' };
  }

  async save() {
    await this.ui.run(async () => {
      const id = this.selected()?.id;
      if (id) await firstValueFrom(this.api.updatePatient(id, this.form));
      else await firstValueFrom(this.api.createPatient(this.form));
      this.reset();
      await this.load();
    }, 'تم حفظ بيانات المريض');
  }

  async remove(id: string) {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.deletePatient(id));
      await this.load();
    }, 'تم حذف المريض');
  }
}
