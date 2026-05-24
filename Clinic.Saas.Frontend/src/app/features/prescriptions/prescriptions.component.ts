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
  lookupId = '';
  form: Record<string, any> = { visitId: '', patientId: '', doctorId: '', notes: '', items: [{ drugName: '', dosage: '', frequency: '', duration: '', route: '', instructions: '' }] };

  async ngOnInit() {
    this.patients.set(await firstValueFrom(this.api.patients()).catch(() => []));
    this.users.set(await firstValueFrom(this.api.users()).catch(() => []));
  }

  doctors() { return this.users().filter((u) => u.role === 'Doctor'); }
  items() { return this.form['items'] as PrescriptionItem[]; }
  addItem() { this.items().push({ drugName: '', dosage: '', frequency: '', duration: '', route: '', instructions: '' }); }
  removeItem(index: number) { this.items().splice(index, 1); }

  async create() {
    await this.ui.run(async () => {
      this.result.set(await firstValueFrom(this.api.createPrescription(this.form)));
    }, 'تم إنشاء الروشتة');
  }

  async loadPrescription() {
    this.result.set(await firstValueFrom(this.api.prescription(this.lookupId)));
  }
}
