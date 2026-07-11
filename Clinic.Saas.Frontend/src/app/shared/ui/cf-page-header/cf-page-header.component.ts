import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'cf-page-header',
  standalone: true,
  template: `
    <header class="cf-page-header">
      <div>
        <h2>{{ title }}</h2>
        @if (description) {
          <p>{{ description }}</p>
        }
      </div>
      @if (actionLabel) {
        <button type="button" class="cf-page-header__action" (click)="action.emit()">{{ actionLabel }}</button>
      }
    </header>
  `,
  styles: [`
    .cf-page-header {
      display: flex;
      align-items: flex-end;
      justify-content: space-between;
      gap: var(--cf-space-4);
    }

    h2 {
      margin: 0;
      color: var(--cf-color-ink);
      font-size: var(--cf-font-32);
      font-weight: var(--cf-weight-bold);
      letter-spacing: 0;
    }

    p {
      margin: var(--cf-space-2) 0 0;
      color: var(--cf-color-muted);
      line-height: 1.7;
    }

    .cf-page-header__action {
      border-color: var(--cf-color-primary);
      background: var(--cf-color-primary);
      color: #fff;
      white-space: nowrap;
    }

    @media (max-width: 760px) {
      .cf-page-header {
        display: grid;
        align-items: stretch;
      }

      h2 {
        font-size: var(--cf-font-24);
      }
    }
  `],
})
export class CfPageHeaderComponent {
  @Input() title = '';
  @Input() description = '';
  @Input() actionLabel = '';
  @Output() action = new EventEmitter<void>();
}
