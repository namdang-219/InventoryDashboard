import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { MonthlySales } from '../../../../core/models/dashboard.model';
import { FormatCurrencyPipe } from '../../../../shared/pipes/format-currency.pipe';

export type SalesRange = '3m' | '6m' | '9m' | '1y';

export interface SalesChartPoint {
  month: string;
  shortLabel: string;
  sold: number;
  revenue: number;
  x: number;
  y: number;
}

@Component({
  selector: 'iid-selling-vehicles-trend',
  standalone: true,
  imports: [FormatCurrencyPipe],
  templateUrl: './selling-vehicles-trend.component.html',
  styleUrl: './selling-vehicles-trend.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SellingVehiclesTrendComponent {
  readonly monthlySales = input<readonly MonthlySales[] | undefined>([]);

  readonly selectedRange = signal<SalesRange>('6m');
  readonly rangeOptions: { label: string; value: SalesRange }[] = [
    { label: '3M', value: '3m' },
    { label: '6M', value: '6m' },
    { label: '9M', value: '9m' },
    { label: '1Y', value: '1y' }
  ];

  readonly hoveredPointIndex = signal<number | null>(null);

  setRange(range: SalesRange): void {
    this.selectedRange.set(range);
    this.hoveredPointIndex.set(null);
  }

  readonly filteredSales = computed(() => {
    const list = this.monthlySales() ?? [];
    const range = this.selectedRange();
    const count = range === '3m' ? 3 : range === '6m' ? 6 : range === '9m' ? 9 : 12;
    return list.slice(-count);
  });

  readonly totalSoldInPeriod = computed(() => {
    return this.filteredSales().reduce((acc, s) => acc + s.sold, 0);
  });

  readonly totalRevenueInPeriod = computed(() => {
    return this.filteredSales().reduce((acc, s) => acc + s.revenue, 0);
  });

  readonly avgSoldPerMonth = computed(() => {
    const l = this.filteredSales();
    return l.length ? (this.totalSoldInPeriod() / l.length).toFixed(1) : '0';
  });

  readonly maxSoldInPeriod = computed(() => {
    const list = this.filteredSales();
    const max = Math.max(0, ...list.map(s => s.sold));
    return Math.max(4, Math.ceil(max * 1.15));
  });

  readonly yGridSteps = computed(() => {
    const max = this.maxSoldInPeriod();
    const mid = Math.round(max / 2);
    return [
      { value: max, y: 25 },
      { value: mid, y: 100 },
      { value: 0, y: 175 }
    ];
  });

  readonly chartPoints = computed<SalesChartPoint[]>(() => {
    const sales = this.filteredSales();
    if (!sales.length) return [];
    const max = this.maxSoldInPeriod();
    const left = 45;
    const right = 25;
    const top = 25;
    const bottom = 35;
    const width = 560 - left - right; // 490
    const height = 210 - top - bottom; // 150
    const n = sales.length;

    return sales.map((s, i) => {
      const x = n === 1 ? left + width / 2 : left + (i / (n - 1)) * width;
      const y = top + height - (s.sold / max) * height;
      const parts = s.month.split(' ');
      const shortLabel = parts[0];
      return {
        month: s.month,
        shortLabel,
        sold: s.sold,
        revenue: s.revenue,
        x: Math.round(x * 10) / 10,
        y: Math.round(y * 10) / 10
      };
    });
  });

  readonly svgLinePath = computed(() => {
    const pts = this.chartPoints();
    if (!pts.length) return '';
    if (pts.length === 1) return `M ${pts[0].x},${pts[0].y} h 1`;

    let path = `M ${pts[0].x},${pts[0].y}`;
    for (let i = 0; i < pts.length - 1; i++) {
      const p0 = pts[i === 0 ? 0 : i - 1];
      const p1 = pts[i];
      const p2 = pts[i + 1];
      const p3 = pts[i + 2 > pts.length - 1 ? pts.length - 1 : i + 2];

      const tension = 0.18;
      const cp1x = p1.x + (p2.x - p0.x) * tension;
      const cp1y = p1.y + (p2.y - p0.y) * tension;
      const cp2x = p2.x - (p3.x - p1.x) * tension;
      const cp2y = p2.y - (p3.y - p1.y) * tension;

      path += ` C ${cp1x.toFixed(1)},${cp1y.toFixed(1)} ${cp2x.toFixed(1)},${cp2y.toFixed(1)} ${p2.x.toFixed(1)},${p2.y.toFixed(1)}`;
    }
    return path;
  });

  readonly svgAreaPath = computed(() => {
    const pts = this.chartPoints();
    if (!pts.length) return '';
    const linePath = this.svgLinePath();
    const baselineY = 175;
    const firstX = pts[0].x;
    const lastX = pts[pts.length - 1].x;
    return `${linePath} L ${lastX},${baselineY} L ${firstX},${baselineY} Z`;
  });

  getTooltipLeft(x: number): number {
    return Math.round((x / 560) * 100);
  }

  getTooltipTop(y: number): number {
    return Math.max(8, Math.round(y - 50));
  }
}
