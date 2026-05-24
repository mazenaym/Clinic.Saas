import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

type Tab = 'reports' | 'inventory' | 'labs' | 'settings' | 'portal';

@Component({
  selector: 'app-operations',
  imports: [FormsModule],
  templateUrl: './operations.component.html',
})
export class OperationsComponent {
  readonly tab = signal<Tab>('reports');
  readonly tabs: { id: Tab; label: string }[] = [
    { id: 'reports', label: 'التقارير' },
    { id: 'inventory', label: 'المخزون' },
    { id: 'labs', label: 'التحاليل' },
    { id: 'settings', label: 'الإعدادات' },
    { id: 'portal', label: 'الحجز الأونلاين' },
  ];

  inventory = [
    { name: 'سرنجات 5ml', category: 'مستلزمات', quantity: 12, min: 20, expiry: '2026-08-01' },
    { name: 'باراسيتامول', category: 'أدوية', quantity: 8, min: 10, expiry: '2026-11-15' },
  ];

  labs = [
    { test: 'CBC', patient: 'مريض تجريبي', status: 'مطلوب', result: '-' },
    { test: 'HbA1c', patient: 'مريض تجريبي', status: 'قيد المعالجة', result: '-' },
  ];

  settings = {
    workingDays: 'السبت - الخميس',
    openTime: '09:00',
    closeTime: '21:00',
    slotDuration: 20,
    consultFee: 300,
    smsEnabled: false,
    whatsappEnabled: true,
  };
}
