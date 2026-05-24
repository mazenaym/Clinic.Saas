import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { Role } from '../../core/models';
import { UiService } from '../../core/ui.service';

interface NavItem {
  label: string;
  icon: string;
  path: string;
  roles?: Role[];
}

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  readonly auth = inject(AuthService);
  readonly ui = inject(UiService);
  private readonly router = inject(Router);

  readonly nav: NavItem[] = [
    { label: 'لوحة التحكم', icon: '⌂', path: '/dashboard' },
    { label: 'المرضى', icon: '+', path: '/patients' },
    { label: 'المواعيد', icon: '◷', path: '/appointments' },
    { label: 'الكشف', icon: '✎', path: '/visits', roles: ['Admin', 'Doctor'] },
    { label: 'الروشتات', icon: 'Rx', path: '/prescriptions', roles: ['Admin', 'Doctor'] },
    { label: 'الفواتير', icon: '$', path: '/billing', roles: ['Admin', 'Reception'] },
    { label: 'تشغيل العيادة', icon: '▦', path: '/operations' },
    { label: 'المستخدمين', icon: '◎', path: '/users', roles: ['Admin'] },
    { label: 'المنصة', icon: '◇', path: '/admin', roles: ['SuperAdmin'] },
  ];

  readonly visibleNav = computed(() => this.nav.filter((item) => !item.roles || this.auth.hasRole(item.roles)));
  readonly sessionState = computed(() => {
    const expiresAt = this.auth.session()?.expiresAt;
    if (!expiresAt) return 'غير معروف';
    const minutes = Math.max(0, Math.ceil((new Date(expiresAt).getTime() - Date.now()) / 60_000));
    return minutes <= 2 ? 'تتجدد الآن تلقائياً' : `تتجدد تلقائياً خلال ${minutes} دقيقة`;
  });

  logout() {
    this.auth.clear();
    this.router.navigateByUrl('/auth');
  }
}
