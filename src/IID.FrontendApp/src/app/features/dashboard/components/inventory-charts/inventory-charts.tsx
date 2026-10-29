import clsx from 'clsx';
import { useMemo } from 'react';
import { InventoryCharts } from '../../../../core/models/dashboard.model';
import './inventory-charts.scss';

export interface InventoryChartsProps {
  charts?: InventoryCharts | null;
}

export function computeMaxAgingCount(charts?: InventoryCharts | null): number {
  const list = charts?.agingHistogram ?? [];
  return Math.max(1, ...list.map(b => b.count));
}

export function computeTotalFuelCount(charts?: InventoryCharts | null): number {
  const list = charts?.fuelBreakdown ?? [];
  return Math.max(1, list.reduce((acc, f) => acc + f.count, 0));
}

export function getAgingHeightPercent(count: number, maxAgingCount: number): number {
  return Math.max(8, Math.round((count / maxAgingCount) * 100));
}

export function getFuelWidthPercent(count: number, totalFuelCount: number): number {
  return Math.max(4, Math.round((count / totalFuelCount) * 100));
}

export function InventoryChartsComponent({ charts = null }: InventoryChartsProps) {
  const maxAgingCount = useMemo(() => computeMaxAgingCount(charts), [charts]);
  const totalFuelCount = useMemo(() => computeTotalFuelCount(charts), [charts]);

  const agingHistogram = charts?.agingHistogram ?? [];
  const fuelBreakdown = charts?.fuelBreakdown ?? [];
  const statusBreakdown = charts?.statusBreakdown ?? [];

  return (
    <div className="iid-inventory-charts charts-grid">
      {/* Chart 1: Aging Buckets Histogram */}
      <div className="chart-card iid-card">
        <div className="chart-header">
          <div className="header-left">
            <span className="material-symbols-outlined icon">hourglass_bottom</span>
            <h4>Aging Stock Histogram</h4>
          </div>
          <span className="header-tag">Lot Days</span>
        </div>

        <div className="histogram-chart">
          {agingHistogram.map(item => (
            <div
              key={item.bucket}
              className={clsx(
                'bar-col',
                (item.bucket.includes('180') || item.bucket.includes('91')) && 'aging-critical'
              )}
            >
              <div className="bar-value">{item.count}</div>
              <div className="bar-track">
                <div
                  className="bar-fill"
                  style={{ height: `${getAgingHeightPercent(item.count, maxAgingCount)}%` }}
                />
              </div>
              <div className="bar-label">{item.bucket}</div>
            </div>
          ))}
        </div>
      </div>

      {/* Chart 3: Fuel Type Breakdown */}
      <div className="chart-card iid-card">
        <div className="chart-header">
          <div className="header-left">
            <span className="material-symbols-outlined icon">local_gas_station</span>
            <h4>Powertrain Distribution</h4>
          </div>
          <span className="header-tag">Fuel Mix</span>
        </div>

        <div className="fuel-breakdown-list">
          {fuelBreakdown.map(fuel => {
            const widthPercent = getFuelWidthPercent(fuel.count, totalFuelCount);
            return (
              <div key={fuel.fuelType} className="fuel-item">
                <div className="fuel-top">
                  <span className="fuel-name">{fuel.fuelType}</span>
                  <span className="fuel-count">
                    {fuel.count} units ({widthPercent}%)
                  </span>
                </div>
                <div className="fuel-progress-track">
                  <div
                    className={clsx('fuel-progress-fill', `fuel-${fuel.fuelType.toLowerCase()}`)}
                    style={{ width: `${widthPercent}%` }}
                  />
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Chart 4: Inventory Status Matrix */}
      <div className="chart-card iid-card">
        <div className="chart-header">
          <div className="header-left">
            <span className="material-symbols-outlined icon">pie_chart</span>
            <h4>Status Breakdown</h4>
          </div>
          <span className="header-tag">Live State</span>
        </div>

        <div className="status-list">
          {statusBreakdown.map(st => (
            <div key={st.status} className="status-row">
              <div className="status-info">
                <span className={clsx('status-bullet', `bullet-${st.status.toLowerCase()}`)} />
                <span className="status-name">{st.status}</span>
              </div>
              <span className="status-count-badge">{st.count} units</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
