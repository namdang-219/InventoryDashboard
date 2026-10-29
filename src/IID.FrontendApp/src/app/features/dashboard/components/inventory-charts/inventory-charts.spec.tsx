import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { InventoryCharts } from '../../../../core/models/dashboard.model';
import {
  InventoryChartsComponent,
  computeMaxAgingCount,
  computeTotalFuelCount,
  getAgingHeightPercent,
  getFuelWidthPercent
} from './inventory-charts';

describe('InventoryChartsComponent', () => {
  const mockCharts: InventoryCharts = {
    statusBreakdown: [
      { status: 'Available', count: 10 },
      { status: 'Sold', count: 5 }
    ],
    fuelBreakdown: [
      { fuelType: 'Petrol', count: 8 },
      { fuelType: 'Electric', count: 4 }
    ],
    agingHistogram: [
      { bucket: '0–30 days', count: 6 },
      { bucket: '31–60 days', count: 4 }
    ],
    monthlySales: []
  };

  it('should create the component', () => {
    const { container } = render(<InventoryChartsComponent charts={mockCharts} />);
    expect(container.querySelector('.iid-inventory-charts')).toBeInTheDocument();
  });

  it('should compute max aging count and percentages', () => {
    const maxAging = computeMaxAgingCount(mockCharts);
    expect(maxAging).toBe(6);
    expect(getAgingHeightPercent(6, maxAging)).toBe(100);

    const { container } = render(<InventoryChartsComponent charts={mockCharts} />);
    const fills = container.querySelectorAll<HTMLElement>('.bar-fill');
    expect(fills).toHaveLength(2);
    expect(fills[0].style.height).toBe('100%');
    expect(fills[1].style.height).toBe('67%');
  });

  it('should compute total fuel count and percentages', () => {
    const totalFuel = computeTotalFuelCount(mockCharts);
    expect(totalFuel).toBe(12);
    expect(getFuelWidthPercent(6, totalFuel)).toBe(50);

    const { container } = render(<InventoryChartsComponent charts={mockCharts} />);
    const fuelFills = container.querySelectorAll<HTMLElement>('.fuel-progress-fill');
    expect(fuelFills).toHaveLength(2);
    expect(fuelFills[0].style.width).toBe('67%');
    expect(fuelFills[1].style.width).toBe('33%');
  });

  it('should render 3 distribution chart cards', () => {
    const { container } = render(<InventoryChartsComponent charts={mockCharts} />);
    const cards = container.querySelectorAll('.chart-card');
    expect(cards.length).toBe(3);
  });
});
