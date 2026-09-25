import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { InventoryCharts } from '../../../../core/models/dashboard.model';

@Component({
  selector: 'iid-inventory-charts',
  standalone: true,
  imports: [],
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

  readonly totalFuelCount = computed(() => {
    const list = this.charts()?.fuelBreakdown ?? [];
    return Math.max(1, list.reduce((acc, f) => acc + f.count, 0));
  });

  getAgingHeightPercent(count: number): number {
    return Math.max(8, Math.round((count / this.maxAgingCount()) * 100));
  }

  getFuelWidthPercent(count: number): number {
    return Math.max(4, Math.round((count / this.totalFuelCount()) * 100));
  }
}
