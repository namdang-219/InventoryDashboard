import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { DashboardSummary, QuickStats } from '../../../../core/models/dashboard.model';
import { AuthService } from '../../../../core/services/auth.service';
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
  private readonly auth = inject(AuthService);

  readonly summary = input<DashboardSummary | null>(null);
  readonly quickStats = input<QuickStats | null>(null);
  readonly isManager = input<boolean>(this.auth.isManager());

  readonly totalValueFormatted = computed(() => {
    const s = this.summary();
    return s ? new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(s.totalInventoryValue) : '$0';
  });

  readonly avgValueFormatted = computed(() => {
    const s = this.summary();
    return s ? new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(s.averageAskingPrice) : '$0';
  });
}
