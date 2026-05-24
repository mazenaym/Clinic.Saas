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
    if (Array.isArray(body?.errors) && body.errors.length) {
      return body.errors.join(' - ');
    }

    if (body?.errors && typeof body.errors === 'object') {
      const errors = Object.values(body.errors).flat().filter(Boolean);
      if (errors.length) {
        return errors.join(' - ');
      }
    }

    return body?.message || error?.message || 'حصل خطأ غير متوقع';
  }
}
