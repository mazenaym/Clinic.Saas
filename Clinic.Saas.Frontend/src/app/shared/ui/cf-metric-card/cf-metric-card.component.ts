import { Component, Input } from '@angular/core';

@Component({
  selector: 'cf-metric-card',
  standalone: true,
  template: `
    <article class="cf-metric-card">
      <div>
        @if (icon) {
          <span class="icon" aria-hidden="true">{{ icon }}</span>
        }
        <span class="label">{{ label }}</span>
      </div>
      <strong>{{ value }}</strong>
    </article>
  `,
  styles: [`
    .cf-metric-card {
      display: grid;
      align-content: space-between;
      min-height: 144px;
      padding: var(--cf-space-5);
      border: var(--cf-border);
      border-radius: var(--cf-radius-card);
      background: var(--cf-color-surface);
      box-shadow: var(--cf-shadow-card);
    }

    .cf-metric-card > div {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--cf-space-3);
    }

    .label {
      color: var(--cf-color-muted);
      font-size: var(--cf-font-14);
    }

    .icon {
      display: grid;
      place-items: center;
      width: 46px;
      height: 46px;
      border-radius: var(--cf-radius-card);
      background: var(--cf-color-info-bg);
      color: var(--cf-color-primary);
      font-size: var(--cf-font-20);
      font-weight: var(--cf-weight-bold);
    }

    strong {
      margin-top: var(--cf-space-3);
      color: var(--cf-color-ink);
      font-size: var(--cf-font-24);
      font-weight: var(--cf-weight-bold);
      line-height: 1.15;
    }
  `],
})
export class CfMetricCardComponent {
  @Input() label = '';
  @Input() value: string | number = '';
  @Input() icon = '';
}
