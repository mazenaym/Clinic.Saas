import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { User, enumValues } from '../../core/models';
import { UiService } from '../../core/ui.service';

@Component({
  selector: 'app-users',
  imports: [FormsModule],
  templateUrl: './users.component.html',
})
export class UsersComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly ui = inject(UiService);
  readonly users = signal<User[]>([]);
  readonly roles = enumValues.roles;
  form: Record<string, any> = { fullName: '', email: '', password: '', role: 2, phone: '', specialty: '', licenseNumber: '' };

  ngOnInit() { this.load(); }

  async load() {
    this.users.set(await firstValueFrom(this.api.users()));
  }

  async create() {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.createUser(this.form));
      this.form = { fullName: '', email: '', password: '', role: 2, phone: '', specialty: '', licenseNumber: '' };
      await this.load();
    }, 'تم إضافة المستخدم');
  }

  async deactivate(user: User) {
    await this.ui.run(async () => {
      await firstValueFrom(this.api.deactivateUser(user.id));
      await this.load();
    }, 'تم تعطيل المستخدم');
  }

  async resetPassword(user: User) {
    const newPassword = prompt('New password');
    if (!newPassword) return;
    await this.ui.run(async () => {
      await firstValueFrom(this.api.resetUserPassword(user.id, newPassword));
    }, 'تم تغيير كلمة المرور');
  }
}
