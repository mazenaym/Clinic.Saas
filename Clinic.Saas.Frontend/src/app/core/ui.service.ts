import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UiService {
  readonly busy = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  async run<T>(work: () => Promise<T>, successMessage = '') {
    this.busy.set(true);
    this.error.set('');
    try {
      const result = await work();
      this.message.set(successMessage);
      return result;
    } catch (error: any) {
      this.error.set(error?.error?.message || error?.message || 'حصل خطأ غير متوقع');
      throw error;
    } finally {
      this.busy.set(false);
    }
  }
}
