import { Injectable, signal } from '@angular/core';

export interface ConfirmDialogState {
  title: string;
  message: string;
  confirmLabel: string;
  cancelLabel: string;
  inputLabel?: string;
  inputPlaceholder?: string;
  inputType?: string;
  required?: boolean;
  resolver: (value: string | null) => void;
}

@Injectable({ providedIn: 'root' })
export class CfConfirmDialogService {
  readonly state = signal<ConfirmDialogState | null>(null);

  confirm(options: Partial<Omit<ConfirmDialogState, 'resolver'>> = {}) {
    return this.open({
      title: options.title ?? 'تأكيد الإجراء',
      message: options.message ?? 'هل تريد المتابعة؟',
      confirmLabel: options.confirmLabel ?? 'تأكيد',
      cancelLabel: options.cancelLabel ?? 'إلغاء',
    });
  }

  prompt(options: Partial<Omit<ConfirmDialogState, 'resolver'>> = {}) {
    return this.open({
      title: options.title ?? 'إدخال مطلوب',
      message: options.message ?? '',
      confirmLabel: options.confirmLabel ?? 'حفظ',
      cancelLabel: options.cancelLabel ?? 'إلغاء',
      inputLabel: options.inputLabel ?? 'القيمة',
      inputPlaceholder: options.inputPlaceholder ?? '',
      inputType: options.inputType ?? 'text',
      required: options.required ?? true,
    });
  }

  close(value: string | null) {
    const current = this.state();
    if (!current) return;
    this.state.set(null);
    current.resolver(value);
  }

  private open(options: Omit<ConfirmDialogState, 'resolver'>) {
    return new Promise<string | null>((resolve) => {
      this.state.set({ ...options, resolver: resolve });
    });
  }
}
