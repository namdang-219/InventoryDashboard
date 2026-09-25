import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DashboardSummary, QuickStats } from '../../../../core/models/dashboard.model';
import { StatCardComponent } from '../../../../shared/components/stat-card/stat-card.component';

@Component({
  selector: 'iid-quick-stats-section',
  standalone: true,
  imports: [StatCardComponent],
  templateUrl: './quick-stats-section.component.html',
  styleUrl: './quick-stats-section.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class QuickStatsSectionComponent {
  readonly summary = input<DashboardSummary | null>(null);
  readonly quickStats = input<QuickStats | null>(null);

  readonly totalValueFormatted = computed(() => {
    const s = this.summary();
    return s ? new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(s.totalInventoryValue) : '$0';
  });

  readonly avgValueFormatted = computed(() => {
    const s = this.summary();
    return s ? new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(s.averageAskingPrice) : '$0';
  });
}
