import { Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { Role } from '../../core/models';
import { UiService } from '../../core/ui.service';
import { MediaService } from '../../core/media.service';

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
export class ShellComponent implements OnDestroy {
  readonly auth = inject(AuthService);
  readonly ui = inject(UiService);
  private readonly router = inject(Router);
  readonly media = inject(MediaService);
  readonly menuOpen = signal(false);
  readonly todayLabel = new Intl.DateTimeFormat('ar-EG', { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date());

  private readonly clinicNav: NavItem[] = [
    { label: 'لوحة القيادة', icon: '▦', path: '/dashboard' },
    { label: 'حسابي', icon: '●', path: '/account', roles: ['Admin', 'Doctor', 'Reception'] },
    { label: 'المرضى', icon: '♙', path: '/patients', roles: ['Admin', 'Doctor', 'Reception'] },
    { label: 'المواعيد', icon: '▣', path: '/appointments', roles: ['Admin', 'Doctor', 'Reception'] },
    { label: 'الحجز الإلكتروني', icon: '↗', path: '/online-bookings', roles: ['Admin', 'Reception'] },
    { label: 'الزيارات', icon: '✚', path: '/visits', roles: ['Admin', 'Doctor'] },
    { label: 'الوصفات الطبية', icon: 'Rx', path: '/prescriptions', roles: ['Admin', 'Doctor'] },
    { label: 'الفواتير', icon: '▤', path: '/billing', roles: ['Admin', 'Reception'] },
    { label: 'التقارير', icon: '▥', path: '/reports', roles: ['Admin', 'Reception'] },
    { label: 'الإعدادات', icon: '⚙', path: '/operations', roles: ['Admin'] },
    { label: 'المستخدمون', icon: '♟', path: '/users', roles: ['Admin'] },
  ];

  private readonly platformNav: NavItem[] = [
    { label: 'لوحة المنصة', icon: '▦', path: '/platform/dashboard' },
    { label: 'العيادات', icon: '✚', path: '/platform/clinics' },
    { label: 'الاشتراكات', icon: '▤', path: '/platform/subscriptions' },
    { label: 'الخطط', icon: '▥', path: '/platform/plans' },
    { label: 'تقارير المنصة', icon: '▣', path: '/platform/reports' },
    { label: 'سجل العمليات', icon: '◷', path: '/platform/audit-logs' },
    { label: 'إعدادات المنصة', icon: '⚙', path: '/platform/settings' },
  ];

  readonly isSuperAdmin = computed(() => this.auth.user()?.role === 'SuperAdmin');
  readonly visibleNav = computed(() => {
    const nav = this.isSuperAdmin() ? this.platformNav : this.clinicNav;
    return nav.filter((item) => !item.roles || this.auth.hasRole(item.roles));
  });
  readonly sessionState = computed(() => {
    const expiresAt = this.auth.session()?.expiresAt;
    if (!expiresAt) return 'جلسة آمنة';
    const minutes = Math.max(0, Math.ceil((new Date(expiresAt).getTime() - Date.now()) / 60_000));
    return minutes <= 2 ? 'يتم تجديد الجلسة الآن' : `جلسة آمنة · ${minutes} د`;
  });
  readonly roleLabel = computed(() => this.roleInArabic(this.auth.user()?.role));
  readonly workspaceName = computed(() => (this.isSuperAdmin() ? 'منصة ClinicFlow' : this.auth.tenant()?.name || 'ClinicFlow'));

  ngOnDestroy() { this.media.clearMediaCache(); }

  avatarFailed() { this.media.avatarLoadFailed(); }
  logoFailed() { this.media.logoLoadFailed(); }

  closeMenu() {
    this.menuOpen.set(false);
  }

  async logout() {
    this.auth.clear();
    await this.router.navigateByUrl('/auth', { replaceUrl: true });
  }

  private roleInArabic(role?: Role) {
    const labels: Record<Role, string> = {
      SuperAdmin: 'مدير المنصة',
      Admin: 'مدير النظام',
      Doctor: 'طبيب',
      Reception: 'موظف استقبال',
    };
    return role ? labels[role] : '';
  }
}
