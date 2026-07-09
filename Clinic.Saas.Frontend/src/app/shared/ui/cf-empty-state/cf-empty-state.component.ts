import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'cf-empty-state',
  standalone: true,
  template: `
    <div class="cf-empty-state">
      <strong>{{ title }}</strong>
      <p>{{ description }}</p>
      @if (actionLabel) {
        <button type="button" (click)="action.emit()">{{ actionLabel }}</button>
      }
    </div>
  `,
  styles: [`
    .cf-empty-state {
      display: grid;
      justify-items: center;
      gap: var(--cf-space-3);
      padding: var(--cf-space-8);
      border: var(--cf-border);
      border-radius: var(--cf-radius-card);
      background: #FBFDFC;
      text-align: center;
    }

    strong {
      color: var(--cf-color-ink);
      font-size: var(--cf-font-18);
      font-weight: var(--cf-weight-medium);
    }

    p {
      max-width: 460px;
      margin: 0;
      color: var(--cf-color-muted);
      line-height: 1.7;
    }

    button {
      border-color: var(--cf-color-primary);
      background: var(--cf-color-primary);
      color: #fff;
    }
  `],
})
export class CfEmptyStateComponent {
  @Input() title = 'لا توجد بيانات';
  @Input() description = 'ستظهر البيانات هنا عند إضافتها.';
  @Input() actionLabel = '';
  @Output() action = new EventEmitter<void>();
}
