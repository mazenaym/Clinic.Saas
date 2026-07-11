import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { UiService } from '../../core/ui.service';
import { enumValues } from '../../core/models';

@Component({
  selector: 'app-auth',
  imports: [FormsModule],
  templateUrl: './auth.component.html',
  styleUrl: './auth.component.scss',
})
export class AuthComponent {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly ui = inject(UiService);
  readonly mode = signal<'login' | 'register'>('login');
  readonly helperMessage = signal('');
  readonly showPassword = signal(false);
  readonly plans = enumValues.plans;

  login = { email: '', password: '', subdomain: '' };
  register = {
    clinicName: '',
    subdomain: '',
    email: '',
    phone: '',
    ownerFullName: '',
    ownerEmail: '',
    ownerPassword: '',
    ownerPhone: '',
    plan: 1,
    timeZone: 'Africa/Cairo',
    currency: 'EGP',
    openTime: '09:00:00',
    closeTime: '21:00:00',
    slotDurationMin: 20,
    consultFee: 0,
    taxPct: 0,
  };

  async submitLogin() {
    this.helperMessage.set('');
    await this.ui.run(async () => {
      const session = await firstValueFrom(this.api.login(this.login));
      this.auth.setSession(session);
      await this.router.navigateByUrl('/dashboard');
    });
  }

  async submitRegister() {
    this.helperMessage.set('');
    await this.ui.run(async () => {
      const session = await firstValueFrom(this.api.registerClinic(this.register));
      this.auth.setSession(session);
      await this.router.navigateByUrl('/dashboard');
    }, 'تم تسجيل العيادة بنجاح');
  }

  showPasswordHelp() {
    this.helperMessage.set('استعادة كلمة المرور لم تكتمل في الـ API بعد. حالياً تواصل مع مدير العيادة أو SuperAdmin لإعادة تعيينها.');
  }
}
