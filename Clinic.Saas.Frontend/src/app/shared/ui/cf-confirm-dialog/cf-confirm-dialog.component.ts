import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CfConfirmDialogService } from './cf-confirm-dialog.service';

@Component({
  selector: 'cf-confirm-dialog',
  standalone: true,
  imports: [FormsModule],
  template: `
    @if (dialog.state(); as state) {
      <div class="backdrop" (click)="dialog.close(null)"></div>
      <section class="dialog" role="dialog" aria-modal="true" [attr.aria-label]="state.title">
        <h2>{{ state.title }}</h2>
        @if (state.message) {
          <p>{{ state.message }}</p>
        }
        @if (state.inputLabel) {
          <label>
            {{ state.inputLabel }}
            @if (state.inputType === 'password') {
              <input [type]="state.inputType" [(ngModel)]="value" [placeholder]="state.inputPlaceholder || ''" />
            } @else {
              <textarea [(ngModel)]="value" [placeholder]="state.inputPlaceholder || ''"></textarea>
            }
          </label>
        }
        <div class="actions">
          <button type="button" class="secondary" (click)="dialog.close(null)">{{ state.cancelLabel }}</button>
          <button type="button" (click)="confirm()" [disabled]="state.required && state.inputLabel && !value.trim()">{{ state.confirmLabel }}</button>
        </div>
      </section>
    }
  `,
  styles: [`
    .backdrop {
      position: fixed;
      inset: 0;
      z-index: 1000;
      background: rgba(20, 38, 43, .32);
    }

    .dialog {
      position: fixed;
      inset: 50% auto auto 50%;
      z-index: 1001;
      display: grid;
      gap: var(--cf-space-4);
      width: min(440px, calc(100vw - 32px));
      padding: var(--cf-space-5);
      transform: translate(-50%, -50%);
      border: var(--cf-border);
      border-radius: var(--cf-radius-card);
      background: var(--cf-color-surface);
    }

    h2 {
      margin: 0;
      color: var(--cf-color-ink);
      font-size: var(--cf-font-18);
      font-weight: var(--cf-weight-medium);
    }

    p {
      margin: 0;
      color: var(--cf-color-muted);
      line-height: 1.7;
    }

    textarea {
      min-height: 92px;
    }

    .actions {
      justify-content: flex-start;
    }
  `],
})
export class CfConfirmDialogComponent {
  readonly dialog = inject(CfConfirmDialogService);
  value = '';

  confirm() {
    const state = this.dialog.state();
    this.dialog.close(state?.inputLabel ? this.value.trim() : 'confirmed');
    this.value = '';
  }
}
