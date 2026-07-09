import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CfConfirmDialogComponent } from './shared/ui';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CfConfirmDialogComponent],
  template: '<router-outlet /><cf-confirm-dialog />',
  styleUrl: './app.scss',
})
export class App {}
