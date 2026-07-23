import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { MediaService } from '../../core/media.service';
import { UiService } from '../../core/ui.service';

@Component({
  selector: 'app-account-settings',
  imports: [FormsModule],
  templateUrl: './account-settings.component.html',
})
export class AccountSettingsComponent implements OnDestroy {
  private readonly api = inject(ApiService);
  readonly auth = inject(AuthService);
  readonly media = inject(MediaService);
  readonly ui = inject(UiService);
  readonly previewUrl = signal<string | null>(null);
  private selectedFile: File | null = null;
  passwordForm = { currentPassword: '', newPassword: '' };
  preferenceForm: Record<string, unknown> = { language: 'ar', theme: 'light' };

  constructor() {
    firstValueFrom(this.api.preferences()).then((value) => this.preferenceForm = { ...this.preferenceForm, ...value }).catch(() => undefined);
  }

  ngOnDestroy() { this.clearPreview(); }

  selectAvatar(event: Event) {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
    this.clearPreview();
    if (this.selectedFile) this.previewUrl.set(URL.createObjectURL(this.selectedFile));
  }

  async uploadAvatar() {
    if (!this.selectedFile) { this.ui.error.set('لم يتم اختيار صورة.'); return; }
    await this.ui.run(async () => {
      await this.media.uploadCurrentAvatar(this.selectedFile!);
      this.selectedFile = null; this.clearPreview();
    }, 'تم تحديث الصورة الشخصية بنجاح.');
  }

  async deleteAvatar() {
    await this.ui.run(() => this.media.deleteCurrentAvatar(), 'تم حذف الصورة الشخصية بنجاح.');
  }

  async savePreferences() {
    await this.ui.run(async () => { this.preferenceForm = await firstValueFrom(this.api.savePreferences(this.preferenceForm)); }, 'تم حفظ تفضيلات الحساب.');
  }

  async changePassword() {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.changePassword(this.passwordForm));
      this.passwordForm = { currentPassword: '', newPassword: '' };
    }, 'تم تغيير كلمة المرور.');
  }

  private clearPreview() {
    const old = this.previewUrl(); if (old) URL.revokeObjectURL(old);
    this.previewUrl.set(null);
  }
}
