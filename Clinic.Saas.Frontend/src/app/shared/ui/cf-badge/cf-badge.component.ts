import { Component, Input } from '@angular/core';

export type CfBadgeVariant = 'success' | 'warning' | 'danger' | 'info' | 'neutral';

@Component({
  selector: 'cf-badge',
  standalone: true,
  template: `<span [class]="variant">{{ label }}</span>`,
  styles: [`
    span {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-height: 28px;
      border-radius: 999px;
      padding: 0 11px;
      font-size: var(--cf-font-12);
      font-weight: var(--cf-weight-medium);
      white-space: nowrap;
    }

    .success {
      background: var(--cf-color-success-bg);
      color: var(--cf-color-success);
    }

    .warning {
      background: var(--cf-color-warning-bg);
      color: var(--cf-color-warning);
    }

    .danger {
      background: var(--cf-color-danger-bg);
      color: var(--cf-color-danger);
    }

    .info {
      background: var(--cf-color-info-bg);
      color: var(--cf-color-info);
    }

    .neutral {
      background: var(--cf-color-neutral-bg);
      color: var(--cf-color-neutral);
    }
  `],
})
export class CfBadgeComponent {
  @Input() label = '';
  @Input() variant: CfBadgeVariant = 'neutral';
}
