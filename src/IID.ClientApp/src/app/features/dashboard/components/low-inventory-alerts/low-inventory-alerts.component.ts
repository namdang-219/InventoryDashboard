import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LowInventoryAlert } from '../../../../core/models/dashboard.model';

@Component({
  selector: 'iid-low-inventory-alerts',
  standalone: true,
  templateUrl: './low-inventory-alerts.component.html',
  styleUrl: './low-inventory-alerts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LowInventoryAlertsComponent {
  readonly alerts = input<readonly LowInventoryAlert[]>([]);
}
