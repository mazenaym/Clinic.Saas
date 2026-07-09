import { Component, Input } from '@angular/core';

@Component({
  selector: 'cf-card',
  standalone: true,
  template: `
    <section class="cf-card">
      @if (title || description) {
        <header>
          @if (title) {
            <h3>{{ title }}</h3>
          }
          @if (description) {
            <p>{{ description }}</p>
          }
        </header>
      }
      <ng-content />
    </section>
  `,
  styles: [`
    .cf-card {
      display: grid;
      gap: var(--cf-space-4);
      padding: var(--cf-space-5);
      border: var(--cf-border);
      border-radius: var(--cf-radius-card);
      background: var(--cf-color-surface);
    }

    h3 {
      margin: 0;
      color: var(--cf-color-ink);
      font-size: var(--cf-font-18);
      font-weight: var(--cf-weight-medium);
    }

    p {
      margin: var(--cf-space-1) 0 0;
      color: var(--cf-color-muted);
      line-height: 1.7;
    }
  `],
})
export class CfCardComponent {
  @Input() title = '';
  @Input() description = '';
}
