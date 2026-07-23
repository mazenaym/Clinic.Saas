import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiResponse } from './models';

@Injectable({ providedIn: 'root' })
export class MediaService {
  private readonly http = inject(HttpClient);
  private identity = '';
  private generation = 0;

  readonly currentAvatarUrl = signal<string | null>(null);
  readonly currentTenantLogoUrl = signal<string | null>(null);
  readonly avatarLoading = signal(false);
  readonly logoLoading = signal(false);
  readonly avatarError = signal('');
  readonly logoError = signal('');

  onSessionChanged(userId?: string, tenantId?: string) {
    const next = userId && tenantId ? `${tenantId}:${userId}` : '';
    if (next === this.identity) return;
    this.clearMediaCache();
    this.identity = next;
    if (next) void Promise.all([this.loadCurrentAvatar(), this.loadTenantLogo()]);
  }

  async loadCurrentAvatar(force = false) {
    if (!this.identity || (!force && this.currentAvatarUrl())) return;
    const generation = this.generation;
    this.avatarLoading.set(true); this.avatarError.set('');
    try {
      const blob = await firstValueFrom(this.http.get('/api/media/me/avatar', { responseType: 'blob' }));
      if (generation === this.generation) this.replaceUrl(this.currentAvatarUrl, blob);
    } catch (error) {
      if (generation === this.generation) {
        this.replaceUrl(this.currentAvatarUrl, null);
        if ((error as HttpErrorResponse)?.status !== 404) this.avatarError.set(this.friendlyError(error, 'تعذر تحميل الصورة الشخصية.'));
      }
    } finally { if (generation === this.generation) this.avatarLoading.set(false); }
  }

  async uploadCurrentAvatar(file: File) {
    this.validateImage(file);
    const form = new FormData(); form.append('file', file);
    this.avatarLoading.set(true); this.avatarError.set('');
    try {
      await firstValueFrom(this.http.post<ApiResponse<unknown>>('/api/media/me/avatar', form));
      await this.loadCurrentAvatar(true);
    } catch (error) { const message = this.friendlyError(error, 'تعذر تحديث الصورة الشخصية.'); this.avatarError.set(message); throw new Error(message); }
    finally { this.avatarLoading.set(false); }
  }

  async deleteCurrentAvatar() {
    this.avatarLoading.set(true); this.avatarError.set('');
    try {
      await firstValueFrom(this.http.delete<ApiResponse<boolean>>('/api/media/me/avatar'));
      this.replaceUrl(this.currentAvatarUrl, null);
    } catch (error) { const message = this.friendlyError(error, 'تعذر حذف الصورة الشخصية.'); this.avatarError.set(message); throw new Error(message); }
    finally { this.avatarLoading.set(false); }
  }

  async loadTenantLogo(force = false) {
    if (!this.identity || (!force && this.currentTenantLogoUrl())) return;
    const generation = this.generation;
    this.logoLoading.set(true); this.logoError.set('');
    try {
      const blob = await firstValueFrom(this.http.get('/api/media/tenant/logo', { responseType: 'blob' }));
      if (generation === this.generation) this.replaceUrl(this.currentTenantLogoUrl, blob);
    } catch (error) {
      if (generation === this.generation) {
        this.replaceUrl(this.currentTenantLogoUrl, null);
        if ((error as HttpErrorResponse)?.status !== 404) this.logoError.set(this.friendlyError(error, 'تعذر تحميل شعار العيادة.'));
      }
    } finally { if (generation === this.generation) this.logoLoading.set(false); }
  }

  async uploadTenantLogo(file: File) {
    this.validateImage(file);
    const form = new FormData(); form.append('file', file);
    this.logoLoading.set(true); this.logoError.set('');
    try {
      await firstValueFrom(this.http.post<ApiResponse<unknown>>('/api/media/tenant/logo', form));
      await this.loadTenantLogo(true);
    } catch (error) { const message = this.friendlyError(error, 'تعذر تحديث شعار العيادة.'); this.logoError.set(message); throw new Error(message); }
    finally { this.logoLoading.set(false); }
  }

  async deleteTenantLogo() {
    this.logoLoading.set(true); this.logoError.set('');
    try {
      await firstValueFrom(this.http.delete<ApiResponse<boolean>>('/api/media/tenant/logo'));
      this.replaceUrl(this.currentTenantLogoUrl, null);
    } catch (error) { const message = this.friendlyError(error, 'تعذر حذف شعار العيادة.'); this.logoError.set(message); throw new Error(message); }
    finally { this.logoLoading.set(false); }
  }

  clearMediaCache() {
    this.generation++;
    this.identity = '';
    this.replaceUrl(this.currentAvatarUrl, null);
    this.replaceUrl(this.currentTenantLogoUrl, null);
    this.avatarLoading.set(false); this.logoLoading.set(false);
    this.avatarError.set(''); this.logoError.set('');
  }

  avatarLoadFailed() { this.replaceUrl(this.currentAvatarUrl, null); this.avatarError.set('تعذر تحميل الصورة الشخصية.'); }
  logoLoadFailed() { this.replaceUrl(this.currentTenantLogoUrl, null); this.logoError.set('تعذر تحميل شعار العيادة.'); }

  private replaceUrl(target: typeof this.currentAvatarUrl, blob: Blob | null) {
    const old = target();
    target.set(blob ? URL.createObjectURL(blob) : null);
    if (old) URL.revokeObjectURL(old);
  }

  private validateImage(file?: File) {
    if (!file) throw new Error('لم يتم اختيار صورة.');
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) throw new Error('نوع الصورة غير مدعوم. استخدم JPG أو PNG أو WebP.');
    if (file.size <= 0) throw new Error('ملف الصورة فارغ.');
    if (file.size > 8 * 1024 * 1024) throw new Error('حجم الصورة أكبر من الحد المسموح وهو 8 ميجابايت.');
  }

  private friendlyError(error: unknown, fallback: string) {
    const http = error as HttpErrorResponse;
    if (http?.status === 401) return 'انتهت الجلسة. سجّل الدخول مرة أخرى.';
    if (http?.status === 403) return 'لا تملك صلاحية تعديل شعار العيادة.';
    if (http?.status === 413) return 'حجم الصورة أكبر من الحد المسموح وهو 8 ميجابايت.';
    if (http?.status === 0) return 'تعذر الاتصال بالخادم. تحقق من الشبكة وحاول مرة أخرى.';
    if (http?.status === 400) return 'نوع الصورة غير مدعوم أو أن ملف الصورة تالف.';
    return fallback;
  }
}
