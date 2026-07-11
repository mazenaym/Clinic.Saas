import { Component, Input } from '@angular/core';

@Component({
  selector: 'cf-avatar',
  standalone: true,
  template: `<span>{{ initials }}</span>`,
  styles: [`
    span {
      display: inline-grid;
      place-items: center;
      width: 38px;
      height: 38px;
      border-radius: 50%;
      background: var(--cf-color-primary-soft);
      color: var(--cf-color-primary);
      font-size: var(--cf-font-13);
      font-weight: var(--cf-weight-medium);
      border: 2px solid rgba(0, 80, 203, .12);
    }
  `],
})
export class CfAvatarComponent {
  @Input() name = '';

  get initials() {
    const parts = this.name.trim().split(/\s+/).filter(Boolean);
    return (parts[0]?.[0] ?? 'C') + (parts[1]?.[0] ?? parts[0]?.[1] ?? 'F');
  }
}
