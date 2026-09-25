import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'iid-loading-spinner',
  standalone: true,
  templateUrl: './loading-spinner.component.html',
  styleUrl: './loading-spinner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadingSpinnerComponent {
  readonly message = input<string>('Loading data...');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
}
