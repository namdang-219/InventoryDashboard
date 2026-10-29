import { fireEvent, render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { MonthlySales } from '../../../../core/models/dashboard.model';
import { SellingVehiclesTrendComponent } from './selling-vehicles-trend';

describe('SellingVehiclesTrendComponent', () => {
  const mockMonthlySales: MonthlySales[] = [
    { month: 'Oct 2025', sold: 3, revenue: 90000 },
    { month: 'Nov 2025', sold: 4, revenue: 120000 },
    { month: 'Dec 2025', sold: 5, revenue: 150000 },
    { month: 'Jan 2026', sold: 2, revenue: 60000 },
    { month: 'Feb 2026', sold: 6, revenue: 180000 },
    { month: 'Mar 2026', sold: 8, revenue: 240000 },
    { month: 'Apr 2026', sold: 7, revenue: 210000 },
    { month: 'May 2026', sold: 9, revenue: 270000 },
    { month: 'Jun 2026', sold: 4, revenue: 120000 },
    { month: 'Jul 2026', sold: 5, revenue: 150000 },
    { month: 'Aug 2026', sold: 6, revenue: 180000 },
    { month: 'Sep 2026', sold: 10, revenue: 300000 }
  ];

  it('should create the component', () => {
    const { container } = render(<SellingVehiclesTrendComponent monthlySales={mockMonthlySales} />);
    expect(container.querySelector('.iid-selling-vehicles-trend')).toBeInTheDocument();
  });

  it('should default selectedRange to 6m', () => {
    const { container } = render(<SellingVehiclesTrendComponent monthlySales={mockMonthlySales} />);
    const pills = container.querySelectorAll('.range-pill');
    expect(pills[1]).toHaveClass('active');

    const dots = container.querySelectorAll('.point-dot');
    const xLabels = container.querySelectorAll('.x-axis-label');
    expect(dots.length).toBe(6);
    expect(xLabels.length).toBe(6);
  });

  it('should filter correctly when 3m is selected', () => {
    const { container } = render(<SellingVehiclesTrendComponent monthlySales={mockMonthlySales} />);
    const pills = container.querySelectorAll('.range-pill');

    fireEvent.click(pills[0]);

    expect(pills[0]).toHaveClass('active');
    const xLabels = container.querySelectorAll('.x-axis-label');
    expect(xLabels.length).toBe(3);
    expect(xLabels[0].textContent?.trim()).toBe('Jul');
    expect(xLabels[2].textContent?.trim()).toBe('Sep');

    const hoverTargets = container.querySelectorAll('.hover-column-target');
    fireEvent.mouseEnter(hoverTargets[0]);
    expect(container.querySelector('.tooltip-month')?.textContent).toBe('Jul 2026');
    fireEvent.mouseEnter(hoverTargets[2]);
    expect(container.querySelector('.tooltip-month')?.textContent).toBe('Sep 2026');

    expect(container.querySelector('.metric-sold')?.textContent).toContain('21');
    expect(container.querySelector('.metric-avg')?.textContent).toContain('7.0');
  });

  it('should filter correctly when 1y is selected', () => {
    const { container } = render(<SellingVehiclesTrendComponent monthlySales={mockMonthlySales} />);
    const pills = container.querySelectorAll('.range-pill');

    fireEvent.click(pills[3]);

    expect(pills[3]).toHaveClass('active');
    const dots = container.querySelectorAll('.point-dot');
    expect(dots.length).toBe(12);
  });

  it('should generate valid SVG path commands for line and area', () => {
    const { container } = render(<SellingVehiclesTrendComponent monthlySales={mockMonthlySales} />);
    const linePath = container.querySelector('.chart-line')?.getAttribute('d') ?? '';
    const areaPath = container.querySelector('.chart-area-fill')?.getAttribute('d') ?? '';

    expect(linePath.startsWith('M')).toBe(true);
    expect(areaPath.startsWith('M')).toBe(true);
    expect(areaPath.endsWith('Z')).toBe(true);
  });

  it('should compute Y grid steps based on max value', () => {
    const { container } = render(<SellingVehiclesTrendComponent monthlySales={mockMonthlySales} />);
    const yLabels = container.querySelectorAll('.y-axis-label');

    expect(yLabels.length).toBe(3);
    expect(Number(yLabels[0].textContent)).toBeGreaterThan(0);
    expect(Number(yLabels[2].textContent)).toBe(0);
  });

  it('should render range pills in template', () => {
    const { container } = render(<SellingVehiclesTrendComponent monthlySales={mockMonthlySales} />);
    const pills = container.querySelectorAll('.range-pill');
    expect(pills.length).toBe(4);
    expect(pills[0].textContent?.trim()).toBe('3M');
    expect(pills[1].textContent?.trim()).toBe('6M');
    expect(pills[2].textContent?.trim()).toBe('9M');
    expect(pills[3].textContent?.trim()).toBe('1Y');
  });
});
