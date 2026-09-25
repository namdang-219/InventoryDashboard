import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'iid-stat-card',
  standalone: true,
  templateUrl: './stat-card.component.html',
  styleUrl: './stat-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatCardComponent {
  readonly title = input.required<string>();
  readonly value = input.required<string | number>();
  readonly icon = input<string>('analytics');
  readonly accent = input<'primary' | 'success' | 'warning' | 'danger' | 'purple'>('primary');
  readonly trend = input<'up' | 'down' | 'flat' | null>(null);
  readonly trendLabel = input<string | null>(null);
  readonly subtitle = input<string | null>(null);

  readonly cardClass = computed(() => `accent-${this.accent()}`);
}
