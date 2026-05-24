import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Appointment, Patient, User, enumValues } from '../../core/models';
import { UiService } from '../../core/ui.service';

@Component({
  selector: 'app-appointments',
  imports: [FormsModule, RouterLink],
  templateUrl: './appointments.component.html',
})
export class AppointmentsComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly ui = inject(UiService);
  readonly appointments = signal<Appointment[]>([]);
  readonly patients = signal<Patient[]>([]);
  readonly users = signal<User[]>([]);
  readonly types = enumValues.appointmentType;
  readonly sources = enumValues.appointmentSource;
  readonly statuses = enumValues.appointmentStatus;
  date = new Date().toISOString().slice(0, 10);
  form: Record<string, any> = { patientId: '', doctorId: '', appointmentDate: this.date, startTime: '09:00:00', endTime: '09:20:00', type: 1, source: 1, notes: '' };

  async ngOnInit() {
    await Promise.all([this.load(), this.loadLookups()]);
  }

  async load() {
    this.appointments.set(await firstValueFrom(this.api.appointments(this.date)));
    this.form['appointmentDate'] = this.date;
  }

  async loadLookups() {
    this.patients.set(await firstValueFrom(this.api.patients()).catch(() => []));
    this.users.set(await firstValueFrom(this.api.users()).catch(() => []));
  }

  doctors() {
    return this.users().filter((u) => u.role === 'Doctor');
  }

  async create() {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.createAppointment(this.form));
      await this.load();
    }, 'تم حجز الموعد');
  }

  async status(id: string, value: number) {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.updateAppointmentStatus(id, value));
      await this.load();
    }, 'تم تحديث حالة الموعد');
  }
}
