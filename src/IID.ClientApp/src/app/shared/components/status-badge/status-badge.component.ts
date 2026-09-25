import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { VehicleStatus } from '../../../core/models/vehicle.model';

@Component({
  selector: 'iid-status-badge',
  standalone: true,
  templateUrl: './status-badge.component.html',
  styleUrl: './status-badge.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatusBadgeComponent {
  readonly status = input<VehicleStatus | string>('Available');

  readonly badgeClass = computed(() => {
    const s = this.status()?.toLowerCase() ?? '';
    switch (s) {
      case 'available':
        return 'status-available';
      case 'sold':
        return 'status-sold';
      case 'pending':
        return 'status-pending';
      case 'wholesale':
        return 'status-wholesale';
      default:
        return 'status-default';
    }
  });

  readonly icon = computed(() => {
    const s = this.status()?.toLowerCase() ?? '';
    switch (s) {
      case 'available':
        return 'check_circle';
      case 'sold':
        return 'monetization_on';
      case 'pending':
        return 'hourglass_top';
      case 'wholesale':
        return 'local_shipping';
      default:
        return 'help';
    }
  });
}
