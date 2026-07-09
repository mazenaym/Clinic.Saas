import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UiService {
  readonly busy = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  async run<T>(work: () => Promise<T>, successMessage = '') {
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      const result = await work();
      this.message.set(successMessage);
      return result;
    } catch (error: any) {
      this.error.set(this.readError(error));
      throw error;
    } finally {
      this.busy.set(false);
    }
  }

  private readError(error: any) {
    const body = error?.error;
    const rawMessage = String(body?.detail || body?.message || body?.title || error?.message || '').trim();

    if (error?.status === 409) {
      return `${this.friendlyKnownError(rawMessage) || 'يوجد تعارض في البيانات.'} حدثت مشكلة، حدّث البيانات وحاول مرة أخرى.`;
    }

    if (Array.isArray(body?.errors) && body.errors.length) {
      return body.errors.map((item: unknown) => this.friendlyKnownError(String(item)) || String(item)).join(' - ');
    }

    if (body?.errors && typeof body.errors === 'object') {
      const errors = Object.values(body.errors).flat().filter(Boolean);
      if (errors.length) {
        return errors.map((item) => this.friendlyKnownError(String(item)) || String(item)).join(' - ');
      }
    }

    if (rawMessage) {
      return this.friendlyKnownError(rawMessage) || rawMessage;
    }

    return 'حدث خطأ غير متوقع. حاول مرة أخرى.';
  }

  private friendlyKnownError(message: string) {
    const normalized = message.toLowerCase();

    if (normalized.includes('appointment conflict') || normalized.includes('not available') || normalized.includes('time slot')) {
      return 'هذا الموعد غير متاح للدكتور المختار.';
    }

    if (normalized.includes('conflict') || normalized.includes('changed by another user')) {
      return 'يوجد تعارض في البيانات.';
    }

    if (normalized.includes('refresh the data and try again')) {
      return 'حدثت مشكلة، حدّث البيانات وحاول مرة أخرى.';
    }

    if (normalized.includes('unauthorized') || normalized.includes('forbidden')) {
      return 'ليس لديك صلاحية لتنفيذ هذا الإجراء.';
    }

    if (normalized.includes('network') || normalized.includes('failed to fetch')) {
      return 'تعذر الاتصال بالخادم. تحقق من الاتصال وحاول مرة أخرى.';
    }

    return '';
  }
}
