import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { Appointment, DailyRevenue, Patient } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [DatePipe, DecimalPipe, RouterLink],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly auth = inject(AuthService);
  readonly today = new Date().toISOString().slice(0, 10);
  readonly appointments = signal<Appointment[]>([]);
  readonly patients = signal<Patient[]>([]);
  readonly revenue = signal<DailyRevenue | null>(null);

  async ngOnInit() {
    await Promise.all([
      firstValueFrom(this.api.appointments(this.today)).then((x) => this.appointments.set(x ?? [])).catch(() => this.appointments.set([])),
      firstValueFrom(this.api.patients()).then((x) => this.patients.set(x ?? [])).catch(() => this.patients.set([])),
      firstValueFrom(this.api.dailyRevenue(this.today)).then((x) => this.revenue.set(x)).catch(() => this.revenue.set(null)),
    ]);
  }
}
