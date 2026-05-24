import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Patient, User, Visit, enumValues } from '../../core/models';
import { UiService } from '../../core/ui.service';

@Component({
  selector: 'app-visits',
  imports: [FormsModule, RouterLink],
  templateUrl: './visits.component.html',
})
export class VisitsComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly ui = inject(UiService);
  readonly patients = signal<Patient[]>([]);
  readonly users = signal<User[]>([]);
  readonly created = signal<Visit | null>(null);
  readonly types = enumValues.visitType;
  lookupId = '';
  form: Record<string, any> = {
    patientId: '', doctorId: '', appointmentId: '', visitType: 1, chiefComplaint: '',
    vitalSigns: { bloodPressure: '', temperature: null, weight: null, height: null, pulse: null, spO2: null, rbs: null },
    clinicalNotes: '', diagnosis: '', diagnosisCode: '', followUpDate: '',
  };

  async ngOnInit() {
    this.patients.set(await firstValueFrom(this.api.patients()).catch(() => []));
    this.users.set(await firstValueFrom(this.api.users()).catch(() => []));
  }

  doctors() { return this.users().filter((u) => u.role === 'Doctor'); }

  async create() {
    await this.ui.run(async () => {
      const payload = { ...this.form, appointmentId: this.form['appointmentId'] || null, followUpDate: this.form['followUpDate'] || null };
      this.created.set(await firstValueFrom(this.api.createVisit(payload)));
    }, 'تم تسجيل الكشف');
  }

  async loadVisit() {
    this.created.set(await firstValueFrom(this.api.visit(this.lookupId)));
  }
}
