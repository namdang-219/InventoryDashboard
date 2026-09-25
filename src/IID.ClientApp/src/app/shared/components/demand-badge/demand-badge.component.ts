import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DemandLevel } from '../../../core/models/vehicle.model';

@Component({
  selector: 'iid-demand-badge',
  standalone: true,
  templateUrl: './demand-badge.component.html',
  styleUrl: './demand-badge.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DemandBadgeComponent {
  readonly level = input<DemandLevel | string>('Medium');
  readonly score = input<number | null>(null);

  readonly levelClass = computed(() => {
    const l = this.level()?.toLowerCase() ?? 'medium';
    return `demand-${l}`;
  });

  readonly icon = computed(() => {
    const l = this.level()?.toLowerCase() ?? 'medium';
    switch (l) {
      case 'high':
        return 'trending_up';
      case 'low':
        return 'trending_down';
      default:
        return 'trending_flat';
    }
  });
}
