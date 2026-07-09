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

  private readonly clinicNav: NavItem[] = [
    { label: 'لوحة اليوم', icon: '⌂', path: '/dashboard' },
    { label: 'المواعيد', icon: '◷', path: '/appointments', roles: ['Admin', 'Doctor', 'Reception'] },
    { label: 'الحجز الأونلاين', icon: '↗', path: '/online-bookings', roles: ['Admin', 'Reception'] },
    { label: 'المرضى', icon: '+', path: '/patients', roles: ['Admin', 'Doctor', 'Reception'] },
    { label: 'الكشف', icon: '✎', path: '/visits', roles: ['Admin', 'Doctor'] },
    { label: 'الروشتات', icon: 'Rx', path: '/prescriptions', roles: ['Admin', 'Doctor'] },
    { label: 'الفواتير التشغيلية', icon: '$', path: '/billing', roles: ['Admin', 'Reception'] },
    { label: 'التقارير', icon: '#', path: '/reports', roles: ['Admin', 'Reception'] },
    { label: 'تشغيل العيادة', icon: '▦', path: '/operations', roles: ['Admin'] },
    { label: 'المستخدمون', icon: '●', path: '/users', roles: ['Admin'] },
  ];

  private readonly platformNav: NavItem[] = [
    { label: 'لوحة المنصة', icon: '◇', path: '/platform/dashboard' },
    { label: 'العيادات', icon: '▦', path: '/platform/clinics' },
    { label: 'الاشتراكات', icon: '$', path: '/platform/subscriptions' },
    { label: 'الخطط', icon: '#', path: '/platform/plans' },
    { label: 'تقارير المنصة', icon: '◷', path: '/platform/reports' },
    { label: 'سجل العمليات', icon: '!', path: '/platform/audit-logs' },
    { label: 'إعدادات المنصة', icon: '*', path: '/platform/settings' },
  ];

  readonly isSuperAdmin = computed(() => this.auth.user()?.role === 'SuperAdmin');
  readonly visibleNav = computed(() => {
    const nav = this.isSuperAdmin() ? this.platformNav : this.clinicNav;
    return nav.filter((item) => !item.roles || this.auth.hasRole(item.roles));
  });
  readonly sessionState = computed(() => {
    const expiresAt = this.auth.session()?.expiresAt;
    if (!expiresAt) return 'غير معروف';
    const minutes = Math.max(0, Math.ceil((new Date(expiresAt).getTime() - Date.now()) / 60_000));
    return minutes <= 2 ? 'تتجدد الآن تلقائيا' : `تتجدد تلقائيا خلال ${minutes} دقيقة`;
  });
  readonly roleLabel = computed(() => this.roleInArabic(this.auth.user()?.role));
  readonly workspaceLabel = computed(() => (this.isSuperAdmin() ? 'المنصة' : 'العيادة'));
  readonly workspaceName = computed(() => (this.isSuperAdmin() ? 'ClinicFlow Platform' : this.auth.tenant()?.name || 'ClinicFlow'));

  logout() {
    this.auth.clear();
    this.router.navigateByUrl('/auth', { replaceUrl: true });
  }

  private roleInArabic(role?: Role) {
    const labels: Record<Role, string> = {
      SuperAdmin: 'مدير المنصة',
      Admin: 'مدير العيادة',
      Doctor: 'طبيب',
      Reception: 'استقبال',
    };
    return role ? labels[role] : '';
  }
}
