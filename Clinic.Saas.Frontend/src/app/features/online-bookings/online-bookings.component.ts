import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Appointment, OnlineBooking, User } from '../../core/models';
import { UiService } from '../../core/ui.service';
import { CfConfirmDialogService } from '../../shared/ui';

@Component({
  selector: 'app-online-bookings',
  imports: [DatePipe, FormsModule, RouterLink],
  templateUrl: './online-bookings.component.html',
})
export class OnlineBookingsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(CfConfirmDialogService);
  readonly ui = inject(UiService);

  readonly bookings = signal<OnlineBooking[]>([]);
  readonly appointments = signal<Appointment[]>([]);
  readonly users = signal<User[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly date = new Date().toISOString().slice(0, 10);
  statusFilter = 'pending';

  readonly visibleBookings = computed(() => {
    const bookings = this.bookings();
    if (this.statusFilter === 'all') return bookings;
    return bookings.filter((booking) => this.isPending(booking));
  });
  readonly pendingCount = computed(() => this.bookings().filter((booking) => this.isPending(booking)).length);
  readonly doctorsCount = computed(() => this.users().filter((user) => user.role === 'Doctor').length);

  async ngOnInit() {
    await this.load();
  }

  async load() {
    this.loading.set(true);
    this.error.set('');
    try {
      const [bookings, users, appointments] = await Promise.all([
        firstValueFrom(this.api.getOnlineBookings()).catch(() => []),
        firstValueFrom(this.api.users()).catch(() => []),
        firstValueFrom(this.api.appointments(this.date)).catch(() => []),
      ]);
      this.bookings.set(bookings ?? []);
      this.users.set(users ?? []);
      this.appointments.set(appointments ?? []);
    } catch {
      this.error.set('تعذر تحميل طلبات الحجز الأونلاين.');
    } finally {
      this.loading.set(false);
    }
  }

  doctorName(booking: OnlineBooking) {
    return booking.doctorName || this.users().find((user) => user.id === booking.doctorId)?.fullName || 'أي دكتور';
  }

  notes(booking: OnlineBooking) {
    return booking.complaint || booking.rejectReason || '-';
  }

  isPending(booking: OnlineBooking) {
    const status = booking.status?.toLowerCase();
    return !status || status === 'pending' || status === 'requested' || status === 'new';
  }

  async approve(booking: OnlineBooking) {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.approveOnlineBooking(booking.id));
      await this.load();
    }, 'تم قبول طلب الحجز');
  }

  async reject(booking: OnlineBooking) {
    const reason = await this.dialog.prompt({
      title: 'رفض طلب الحجز',
      message: `اكتب سبب رفض طلب ${booking.patientName}.`,
      inputLabel: 'سبب الرفض',
      confirmLabel: 'رفض الطلب',
    });
    if (!reason?.trim()) return;
    await this.ui.run(async () => {
      await firstValueFrom(this.api.rejectOnlineBooking(booking.id, reason));
      await this.load();
    }, 'تم رفض طلب الحجز');
  }
}
