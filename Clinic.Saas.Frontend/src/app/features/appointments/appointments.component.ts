import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Appointment, Patient, TimeSlot, User, enumValues } from '../../core/models';
import { UiService } from '../../core/ui.service';
import { CfBadgeComponent, CfBadgeVariant, CfCardComponent, CfEmptyStateComponent, CfMetricCardComponent, CfPageHeaderComponent, CfConfirmDialogService } from '../../shared/ui';

@Component({
  selector: 'app-appointments',
  imports: [FormsModule, RouterLink, CfPageHeaderComponent, CfCardComponent, CfBadgeComponent, CfMetricCardComponent, CfEmptyStateComponent],
  templateUrl: './appointments.component.html',
})
export class AppointmentsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(CfConfirmDialogService);
  readonly ui = inject(UiService);
  readonly appointments = signal<Appointment[]>([]);
  readonly rangeAppointments = signal<Appointment[]>([]);
  readonly monthlyAppointments = signal<Appointment[]>([]);
  readonly onlineBookings = signal<Record<string, unknown>[]>([]);
  readonly cancellations = signal<Appointment[]>([]);
  readonly patients = signal<Patient[]>([]);
  readonly users = signal<User[]>([]);
  readonly availabilitySlots = signal<TimeSlot[]>([]);
  readonly availabilityLoading = signal(false);
  readonly availabilityError = signal('');
  readonly availabilityLoaded = signal(false);
  readonly types = enumValues.appointmentType;
  readonly sources = enumValues.appointmentSource;
  readonly statuses = enumValues.appointmentStatus;
  readonly pendingOnlineBookings = () => this.onlineBookings().filter((booking) => {
    const status = String(booking['status'] || '').toLowerCase();
    return !status || status === 'pending' || status === 'requested' || status === 'new';
  }).length;
  date = new Date().toISOString().slice(0, 10);
  reschedule: Record<string, any> = { id: '', appointmentDate: this.date, startTime: '09:00:00', endTime: '09:20:00' };
  form: Record<string, any> = { patientId: '', doctorId: '', appointmentDate: this.date, startTime: '09:00:00', endTime: '09:20:00', type: 1, source: 1, notes: '' };
  private availabilityRequestId = 0;

  async ngOnInit() {
    await Promise.all([this.load(), this.loadLookups(), this.loadExtras()]);
  }

  async load() {
    this.appointments.set(await firstValueFrom(this.api.appointments(this.date)));
    this.form['appointmentDate'] ||= this.date;
    await this.refreshAvailability();
  }

  async loadLookups() {
    this.patients.set(await firstValueFrom(this.api.patients()).catch(() => []));
    this.users.set(await firstValueFrom(this.api.users()).catch(() => []));
    this.applyDefaultLookups();
    await this.refreshAvailability();
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
  statusVariant(status: string): CfBadgeVariant {
    const normalized = status?.toLowerCase() ?? '';
    if (normalized.includes('complete') || normalized.includes('مكتمل')) return 'success';
    if (normalized.includes('cancel') || normalized.includes('ملغي')) return 'danger';
    if (normalized.includes('confirm') || normalized.includes('مؤكد')) return 'info';
    if (normalized.includes('no') || normalized.includes('لم يحضر')) return 'warning';
    return 'neutral';
  }

  private applyDefaultLookups() {
    this.form['patientId'] ||= this.patients()[0]?.id || '';
    this.form['doctorId'] ||= this.doctors()[0]?.id || '';
    this.form['appointmentDate'] ||= this.date;
  }

  async appointmentCriteriaChanged() {
    this.form['startTime'] = '';
    this.form['endTime'] = '';
    await this.refreshAvailability();
  }

  async refreshAvailability() {
    const doctorId = this.form['doctorId'];
    const appointmentDate = this.form['appointmentDate'];
    this.availabilityError.set('');
    this.availabilityLoaded.set(false);
    this.availabilitySlots.set([]);

    if (!doctorId || !appointmentDate) return;

    const requestId = ++this.availabilityRequestId;
    this.availabilityLoading.set(true);
    try {
      const duration = this.selectedDurationMinutes();
      const availability = await firstValueFrom(this.api.getAppointmentAvailability(doctorId, appointmentDate, duration));
      if (requestId !== this.availabilityRequestId) return;
      this.availabilitySlots.set((availability.availableSlots ?? []).filter((slot) => slot.isAvailable));
      this.availabilityLoaded.set(true);
    } catch {
      if (requestId !== this.availabilityRequestId) return;
      this.availabilityError.set('تعذر تحميل المواعيد المتاحة.');
      this.availabilityLoaded.set(true);
    } finally {
      if (requestId === this.availabilityRequestId) this.availabilityLoading.set(false);
    }
  }

  pickSlot(slot: TimeSlot) {
    if (!slot.isAvailable) return;
    this.form['startTime'] = this.normalizeTime(slot.startTime);
    this.form['endTime'] = this.normalizeTime(slot.endTime);
  }

  isPickedSlot(slot: TimeSlot) {
    return this.normalizeTime(slot.startTime) === this.form['startTime'] && this.normalizeTime(slot.endTime) === this.form['endTime'];
  }

  canCreateAppointment() {
    return Boolean(this.form['patientId'] && this.form['doctorId'] && this.form['appointmentDate'] && this.form['startTime'] && this.form['endTime'] && !this.ui.busy());
  }

  async create() {
    await this.ui.run(async () => {
      this.applyDefaultLookups();
      await firstValueFrom(this.api.createAppointment(this.form));
      await this.load();
      await this.loadExtras();
    }, 'تم حجز الموعد');
  }

  async status(id: string, value: number) {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.updateAppointmentStatus(id, value, undefined, this.findAppointment(id)?.rowVersion));
      await this.load();
      await this.loadExtras();
    }, 'تم تحديث حالة الموعد');
  }

  pickReschedule(a: Appointment) {
    this.reschedule = { id: a.id, appointmentDate: a.appointmentDate?.slice(0, 10) || this.date, startTime: a.startTime, endTime: a.endTime, rowVersion: a.rowVersion };
  }

  async saveReschedule() {
    if (!this.reschedule['id']) return;
    await this.ui.run(async () => {
      const id = this.reschedule['id'];
      const rowVersion = this.reschedule['rowVersion'] || this.findAppointment(id)?.rowVersion;
      await firstValueFrom(this.api.rescheduleAppointment(id, { ...this.reschedule, rowVersion }));
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
    const rejectReason = await this.dialog.prompt({
      title: 'رفض طلب الحجز',
      message: 'اكتب سبب الرفض ليتم حفظه مع الطلب.',
      inputLabel: 'سبب الرفض',
      confirmLabel: 'رفض الطلب',
    });
    if (!rejectReason) return;
    await this.ui.run(async () => {
      await firstValueFrom(this.api.rejectOnlineBooking(id, rejectReason));
      await this.loadExtras();
    }, 'تم رفض طلب الحجز');
  }

  private findAppointment(id: string) {
    return [...this.appointments(), ...this.rangeAppointments(), ...this.monthlyAppointments(), ...this.cancellations()].find((appointment) => appointment.id === id);
  }

  private selectedDurationMinutes() {
    const start = this.form['startTime'];
    const end = this.form['endTime'];
    if (!start || !end) return undefined;
    const startMinutes = this.timeToMinutes(start);
    const endMinutes = this.timeToMinutes(end);
    const duration = endMinutes - startMinutes;
    return duration > 0 ? duration : undefined;
  }

  private normalizeTime(value: string) {
    const parts = value.split(':');
    if (parts.length === 2) return `${value}:00`;
    return value;
  }

  private timeToMinutes(value: string) {
    const [hours, minutes] = value.split(':').map((part) => Number(part));
    return hours * 60 + minutes;
  }
}
