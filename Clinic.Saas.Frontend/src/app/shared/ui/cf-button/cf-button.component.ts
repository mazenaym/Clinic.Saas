import { Component, EventEmitter, Input, Output } from '@angular/core';

type ButtonVariant = 'primary' | 'secondary' | 'danger';
type ButtonType = 'button' | 'submit' | 'reset';

@Component({
  selector: 'cf-button',
  standalone: true,
  template: `
    <button [type]="type" [class]="variant" [disabled]="disabled" (click)="pressed.emit()">
      {{ label }}
    </button>
  `,
  styles: [`
    button {
      min-height: 40px;
      border: 2px solid transparent;
      border-radius: var(--cf-radius-control);
      padding: 0 var(--cf-space-4);
      font-weight: var(--cf-weight-medium);
      cursor: pointer;
    }

    .primary {
      border-color: var(--cf-color-primary);
      background: var(--cf-color-primary);
      color: #fff;
    }

    .secondary {
      border-color: var(--cf-color-border);
      background: var(--cf-color-surface);
      color: var(--cf-color-ink);
    }

    .danger {
      border-color: var(--cf-color-danger);
      background: var(--cf-color-danger);
      color: #fff;
    }

    button:focus-visible {
      outline: none;
      box-shadow: var(--cf-focus-ring);
    }
  `],
})
export class CfButtonComponent {
  @Input() label = '';
  @Input() variant: ButtonVariant = 'secondary';
  @Input() type: ButtonType = 'button';
  @Input() disabled = false;
  @Output() pressed = new EventEmitter<void>();
}
