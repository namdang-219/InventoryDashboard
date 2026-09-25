import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { InventoryCharts } from '../../../../core/models/dashboard.model';
import { FormatCurrencyPipe } from '../../../../shared/pipes/format-currency.pipe';

@Component({
  selector: 'iid-inventory-charts',
  standalone: true,
  imports: [FormatCurrencyPipe],
  templateUrl: './inventory-charts.component.html',
  styleUrl: './inventory-charts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InventoryChartsComponent {
  readonly charts = input<InventoryCharts | null>(null);

  readonly maxAgingCount = computed(() => {
    const list = this.charts()?.agingHistogram ?? [];
    return Math.max(1, ...list.map(b => b.count));
  });

  readonly maxMonthlyRevenue = computed(() => {
    const list = this.charts()?.monthlySales ?? [];
    return Math.max(1, ...list.map(s => s.revenue));
  });

  readonly totalFuelCount = computed(() => {
    const list = this.charts()?.fuelBreakdown ?? [];
    return Math.max(1, list.reduce((acc, f) => acc + f.count, 0));
  });

  getAgingHeightPercent(count: number): number {
    return Math.max(8, Math.round((count / this.maxAgingCount()) * 100));
  }

  getRevenueHeightPercent(revenue: number): number {
    return Math.max(10, Math.round((revenue / this.maxMonthlyRevenue()) * 100));
  }

  getFuelWidthPercent(count: number): number {
    return Math.max(4, Math.round((count / this.totalFuelCount()) * 100));
  }
}
