import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { AgingSeverity } from '../../../core/models/vehicle.model';

@Component({
  selector: 'iid-aging-severity-badge',
  standalone: true,
  templateUrl: './aging-severity-badge.component.html',
  styleUrl: './aging-severity-badge.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AgingSeverityBadgeComponent {
  readonly severity = input<AgingSeverity | string>('None');
  readonly days = input<number | null>(null);

  readonly severityClass = computed(() => {
    const s = this.severity()?.toLowerCase() ?? 'none';
    return `severity-${s}`;
  });

  readonly label = computed(() => {
    const s = this.severity() ?? 'None';
    const d = this.days();
    return d !== null ? `${d}d (${s})` : s;
  });
}
