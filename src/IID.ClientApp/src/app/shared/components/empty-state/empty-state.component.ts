import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'iid-empty-state',
  standalone: true,
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmptyStateComponent {
  readonly icon = input<string>('inbox');
  readonly title = input<string>('No records found');
  readonly description = input<string>('There are no items to display at this time.');
  readonly actionLabel = input<string | null>(null);

  readonly actionClick = output<void>();
}
