import { Component, Input } from '@angular/core';

@Component({
  selector: 'cf-metric-card',
  standalone: true,
  template: `
    <article class="cf-metric-card">
      <span>{{ label }}</span>
      <strong>{{ value }}</strong>
    </article>
  `,
  styles: [`
    .cf-metric-card {
      display: grid;
      align-content: space-between;
      min-height: 116px;
      padding: var(--cf-space-5);
      border: var(--cf-border);
      border-radius: var(--cf-radius-card);
      background: var(--cf-color-surface);
    }

    span {
      color: var(--cf-color-muted);
      font-size: var(--cf-font-13);
    }

    strong {
      margin-top: var(--cf-space-3);
      color: var(--cf-color-ink);
      font-size: var(--cf-font-24);
      font-weight: var(--cf-weight-medium);
      line-height: 1.15;
    }
  `],
})
export class CfMetricCardComponent {
  @Input() label = '';
  @Input() value: string | number = '';
}
