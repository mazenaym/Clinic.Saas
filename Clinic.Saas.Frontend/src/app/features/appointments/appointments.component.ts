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
  readonly rangeAppointments = signal<Appointment[]>([]);
  readonly monthlyAppointments = signal<Appointment[]>([]);
  readonly onlineBookings = signal<Record<string, unknown>[]>([]);
  readonly cancellations = signal<Appointment[]>([]);
  readonly patients = signal<Patient[]>([]);
  readonly users = signal<User[]>([]);
  readonly types = enumValues.appointmentType;
  readonly sources = enumValues.appointmentSource;
  readonly statuses = enumValues.appointmentStatus;
  date = new Date().toISOString().slice(0, 10);
  reschedule: Record<string, any> = { id: '', appointmentDate: this.date, startTime: '09:00:00', endTime: '09:20:00' };
  form: Record<string, any> = { patientId: '', doctorId: '', appointmentDate: this.date, startTime: '09:00:00', endTime: '09:20:00', type: 1, source: 1, notes: '' };

  async ngOnInit() {
    await Promise.all([this.load(), this.loadLookups(), this.loadExtras()]);
  }

  async load() {
    this.appointments.set(await firstValueFrom(this.api.appointments(this.date)));
    this.form['appointmentDate'] = this.date;
  }

  async loadLookups() {
    this.patients.set(await firstValueFrom(this.api.patients()).catch(() => []));
    this.users.set(await firstValueFrom(this.api.users()).catch(() => []));
    this.applyDefaultLookups();
  }

  async loadExtras() {
    const start = new Date(this.date);
    const end = new Date(start.getFullYear(), start.getMonth() + 1, 0).toISOString().slice(0, 10);
    await Promise.all([
      firstValueFrom(this.api.weeklyAppointments(this.date)).then((x) => this.rangeAppointments.set(x ?? [])).catch(() => this.rangeAppointments.set([])),
      firstValueFrom(this.api.monthlyAppointments(start.getFullYear(), start.getMonth() + 1)).then((x) => this.monthlyAppointments.set(x ?? [])).catch(() => this.monthlyAppointments.set([])),
      firstValueFrom(this.api.onlineBookings()).then((x) => this.onlineBookings.set(x ?? [])).catch(() => this.onlineBookings.set([])),
      firstValueFrom(this.api.cancellationReport(this.date, end)).then((x) => this.cancellations.set(x ?? [])).catch(() => this.cancellations.set([])),
    ]);
  }

  doctors() { return this.users().filter((u) => u.role === 'Doctor'); }
  asString(value: unknown) { return String(value); }
  statusValue(status: string) {
    const normalized = status?.toLowerCase();
    const map: Record<string, number> = { scheduled: 1, confirmed: 2, completed: 3, cancelled: 4, canceled: 4, noshow: 5, 'no-show': 5 };
    return map[normalized] ?? 1;
  }

  private applyDefaultLookups() {
    this.form['patientId'] ||= this.patients()[0]?.id || '';
    this.form['doctorId'] ||= this.doctors()[0]?.id || '';
  }

  async create() {
    await this.ui.run(async () => {
      this.applyDefaultLookups();
      this.form['appointmentDate'] = this.date;
      await firstValueFrom(this.api.createAppointment(this.form));
      await this.load();
      await this.loadExtras();
    }, 'تم حجز الموعد');
  }

  async status(id: string, value: number) {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.updateAppointmentStatus(id, value));
      await this.load();
      await this.loadExtras();
    }, 'تم تحديث حالة الموعد');
  }

  pickReschedule(a: Appointment) {
    this.reschedule = { id: a.id, appointmentDate: a.appointmentDate?.slice(0, 10) || this.date, startTime: a.startTime, endTime: a.endTime };
  }

  async saveReschedule() {
    if (!this.reschedule['id']) return;
    await this.ui.run(async () => {
      await firstValueFrom(this.api.rescheduleAppointment(this.reschedule['id'], this.reschedule));
      await this.load();
      await this.loadExtras();
    }, 'تم تغيير الموعد');
  }

  async approveBooking(id: string) {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.approveOnlineBooking(id));
      await this.loadExtras();
    }, 'تم قبول طلب الحجز');
  }

  async rejectBooking(id: string) {
    const rejectReason = prompt('سبب الرفض') || '';
    await this.ui.run(async () => {
      await firstValueFrom(this.api.rejectOnlineBooking(id, rejectReason));
      await this.loadExtras();
    }, 'تم رفض طلب الحجز');
  }
}
