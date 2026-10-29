import clsx from 'clsx';
import { Fragment, useMemo, useState } from 'react';
import { MonthlySales } from '../../../../core/models/dashboard.model';
import { formatCurrency } from '../../../../shared/pipes/format-currency.pipe';
import './selling-vehicles-trend.scss';

export type SalesRange = '3m' | '6m' | '9m' | '1y';

export interface SalesChartPoint {
  month: string;
  shortLabel: string;
  sold: number;
  revenue: number;
  x: number;
  y: number;
}

export interface SellingVehiclesTrendProps {
  monthlySales?: readonly MonthlySales[];
}

const RANGE_OPTIONS: { label: string; value: SalesRange }[] = [
  { label: '3M', value: '3m' },
  { label: '6M', value: '6m' },
  { label: '9M', value: '9m' },
  { label: '1Y', value: '1y' }
];

function getTooltipLeft(x: number): number {
  return Math.round((x / 560) * 100);
}

function getTooltipTop(y: number): number {
  return Math.max(8, Math.round(y - 50));
}

export function SellingVehiclesTrendComponent({ monthlySales = [] }: SellingVehiclesTrendProps) {
  const [selectedRange, setSelectedRange] = useState<SalesRange>('6m');
  const [hoveredPointIndex, setHoveredPointIndex] = useState<number | null>(null);

  const handleSetRange = (range: SalesRange) => {
    setSelectedRange(range);
    setHoveredPointIndex(null);
  };

  const filteredSales = useMemo(() => {
    const list = monthlySales ?? [];
    const count =
      selectedRange === '3m' ? 3 : selectedRange === '6m' ? 6 : selectedRange === '9m' ? 9 : 12;
    return list.slice(-count);
  }, [monthlySales, selectedRange]);

  const totalSoldInPeriod = useMemo(() => {
    return filteredSales.reduce((acc, s) => acc + s.sold, 0);
  }, [filteredSales]);

  const totalRevenueInPeriod = useMemo(() => {
    return filteredSales.reduce((acc, s) => acc + s.revenue, 0);
  }, [filteredSales]);

  const avgSoldPerMonth = useMemo(() => {
    return filteredSales.length ? (totalSoldInPeriod / filteredSales.length).toFixed(1) : '0';
  }, [filteredSales, totalSoldInPeriod]);

  const maxSoldInPeriod = useMemo(() => {
    const max = Math.max(0, ...filteredSales.map(s => s.sold));
    return Math.max(4, Math.ceil(max * 1.15));
  }, [filteredSales]);

  const yGridSteps = useMemo(() => {
    const mid = Math.round(maxSoldInPeriod / 2);
    return [
      { value: maxSoldInPeriod, y: 25 },
      { value: mid, y: 100 },
      { value: 0, y: 175 }
    ];
  }, [maxSoldInPeriod]);

  const chartPoints = useMemo<SalesChartPoint[]>(() => {
    if (!filteredSales.length) return [];
    const max = maxSoldInPeriod;
    const left = 45;
    const right = 25;
    const top = 25;
    const bottom = 35;
    const width = 560 - left - right; // 490
    const height = 210 - top - bottom; // 150
    const n = filteredSales.length;

    return filteredSales.map((s, i) => {
      const x = n === 1 ? left + width / 2 : left + (i / (n - 1)) * width;
      const y = top + height - (s.sold / max) * height;
      const parts = s.month.split(' ');
      const shortLabel = parts[0] ?? '';
      return {
        month: s.month,
        shortLabel,
        sold: s.sold,
        revenue: s.revenue,
        x: Math.round(x * 10) / 10,
        y: Math.round(y * 10) / 10
      };
    });
  }, [filteredSales, maxSoldInPeriod]);

  const svgLinePath = useMemo(() => {
    if (!chartPoints.length) return '';
    if (chartPoints.length === 1) return `M ${chartPoints[0].x},${chartPoints[0].y} h 1`;

    let path = `M ${chartPoints[0].x},${chartPoints[0].y}`;
    for (let i = 0; i < chartPoints.length - 1; i++) {
      const p0 = chartPoints[i === 0 ? 0 : i - 1];
      const p1 = chartPoints[i];
      const p2 = chartPoints[i + 1];
      const p3 = chartPoints[i + 2 > chartPoints.length - 1 ? chartPoints.length - 1 : i + 2];

      const tension = 0.18;
      const cp1x = p1.x + (p2.x - p0.x) * tension;
      const cp1y = p1.y + (p2.y - p0.y) * tension;
      const cp2x = p2.x - (p3.x - p1.x) * tension;
      const cp2y = p2.y - (p3.y - p1.y) * tension;

      path += ` C ${cp1x.toFixed(1)},${cp1y.toFixed(1)} ${cp2x.toFixed(1)},${cp2y.toFixed(1)} ${p2.x.toFixed(1)},${p2.y.toFixed(1)}`;
    }
    return path;
  }, [chartPoints]);

  const svgAreaPath = useMemo(() => {
    if (!chartPoints.length) return '';
    const baselineY = 175;
    const firstX = chartPoints[0].x;
    const lastX = chartPoints[chartPoints.length - 1].x;
    return `${svgLinePath} L ${lastX},${baselineY} L ${firstX},${baselineY} Z`;
  }, [chartPoints, svgLinePath]);

  const activePt = hoveredPointIndex !== null ? (chartPoints[hoveredPointIndex] ?? null) : null;

  return (
    <div className="iid-selling-vehicles-trend sales-trend-card iid-card">
      {/* Card Header */}
      <div className="trend-header">
        <div className="header-left">
          <div className="header-title-row">
            <span className="material-symbols-outlined icon">trending_up</span>
            <h3>Selling Vehicles Trend</h3>
          </div>
          <div className="header-metrics">
            <span className="metric-sold">
              <strong>{totalSoldInPeriod}</strong> sold
            </span>
            <span className="metric-divider">&bull;</span>
            <span className="metric-avg">
              avg <strong>{avgSoldPerMonth}</strong>/mo
            </span>
            {totalRevenueInPeriod > 0 && (
              <>
                <span className="metric-divider">&bull;</span>
                <span className="metric-rev">{formatCurrency(totalRevenueInPeriod)}</span>
              </>
            )}
          </div>
        </div>

        {/* Range Selector: 3m - 6m - 9m - 1 year */}
        <div className="range-selector">
          {RANGE_OPTIONS.map(opt => (
            <button
              key={opt.value}
              type="button"
              className={clsx('range-pill', selectedRange === opt.value && 'active')}
              onClick={() => handleSetRange(opt.value)}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>

      {chartPoints.length === 0 ? (
        <div className="chart-empty">No recent vehicle sales recorded for this period.</div>
      ) : (
        <div className="line-chart-container">
          <svg
            className="line-chart-svg"
            viewBox="0 0 560 210"
            preserveAspectRatio="none"
            onMouseLeave={() => setHoveredPointIndex(null)}
          >
            <defs>
              <linearGradient id="splitSalesGradient" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#0284c7" stopOpacity="0.38" />
                <stop offset="55%" stopColor="#38bdf8" stopOpacity="0.12" />
                <stop offset="100%" stopColor="#38bdf8" stopOpacity="0.0" />
              </linearGradient>
            </defs>

            {/* Horizontal Grid Lines and Y-Axis Labels */}
            {yGridSteps.map(step => (
              <Fragment key={step.y}>
                <line x1="45" y1={step.y} x2="535" y2={step.y} className="grid-line" />
                <text x="38" y={step.y + 4} className="y-axis-label" textAnchor="end">
                  {step.value}
                </text>
              </Fragment>
            ))}

            {/* Gradient Area Fill */}
            <path d={svgAreaPath} fill="url(#splitSalesGradient)" className="chart-area-fill" />

            {/* Main Smooth Curve Stroke Line */}
            <path d={svgLinePath} className="chart-line" />

            {/* Data Points & Interactive Target Columns */}
            {chartPoints.map((pt, idx) => (
              <Fragment key={pt.month}>
                {/* Hover column target */}
                <rect
                  x={pt.x - 20}
                  y="15"
                  width="40"
                  height="165"
                  className="hover-column-target"
                  onMouseEnter={() => setHoveredPointIndex(idx)}
                />

                {/* Vertical guide line on active hover */}
                {hoveredPointIndex === idx && (
                  <>
                    <line x1={pt.x} y1="25" x2={pt.x} y2="175" className="hover-guide-line" />
                    <circle cx={pt.x} cy={pt.y} r="7" className="point-ring" />
                  </>
                )}

                {/* Point Inner Dot */}
                <circle
                  cx={pt.x}
                  cy={pt.y}
                  r={hoveredPointIndex === idx ? 4.5 : 3.5}
                  className={clsx('point-dot', hoveredPointIndex === idx && 'point-dot-active')}
                />

                {/* X-Axis Month Labels */}
                <text
                  x={pt.x}
                  y="196"
                  className={clsx('x-axis-label', hoveredPointIndex === idx && 'label-active')}
                  textAnchor="middle"
                >
                  {pt.shortLabel}
                </text>
              </Fragment>
            ))}
          </svg>

          {/* Tooltip overlay */}
          {activePt && (
            <div
              className="chart-tooltip"
              style={{
                left: `${getTooltipLeft(activePt.x)}%`,
                top: `${getTooltipTop(activePt.y)}px`
              }}
            >
              <div className="tooltip-month">{activePt.month}</div>
              <div className="tooltip-sold">
                <span className="sold-val">{activePt.sold}</span> vehicles sold
              </div>
              {activePt.revenue > 0 && (
                <div className="tooltip-rev">{formatCurrency(activePt.revenue)} revenue</div>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
