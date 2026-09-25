import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SellingVehiclesTrendComponent } from './selling-vehicles-trend.component';
import { MonthlySales } from '../../../../core/models/dashboard.model';

describe('SellingVehiclesTrendComponent', () => {
  let component: SellingVehiclesTrendComponent;
  let fixture: ComponentFixture<SellingVehiclesTrendComponent>;

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

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SellingVehiclesTrendComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(SellingVehiclesTrendComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('monthlySales', mockMonthlySales);
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should default selectedRange to 6m', () => {
    expect(component.selectedRange()).toBe('6m');
    expect(component.filteredSales().length).toBe(6);
    expect(component.chartPoints().length).toBe(6);
  });

  it('should filter correctly when 3m is selected', () => {
    component.setRange('3m');
    fixture.detectChanges();

    expect(component.selectedRange()).toBe('3m');
    const filtered = component.filteredSales();
    expect(filtered.length).toBe(3);
    expect(filtered[0].month).toBe('Jul 2026');
    expect(filtered[2].month).toBe('Sep 2026');

    expect(component.totalSoldInPeriod()).toBe(21);
    expect(component.avgSoldPerMonth()).toBe('7.0');
  });

  it('should filter correctly when 1y is selected', () => {
    component.setRange('1y');
    fixture.detectChanges();

    expect(component.selectedRange()).toBe('1y');
    expect(component.filteredSales().length).toBe(12);
  });

  it('should generate valid SVG path commands for line and area', () => {
    expect(component.svgLinePath().startsWith('M')).toBeTrue();
    expect(component.svgAreaPath().startsWith('M')).toBeTrue();
    expect(component.svgAreaPath().endsWith('Z')).toBeTrue();
  });

  it('should compute Y grid steps based on max value', () => {
    const steps = component.yGridSteps();
    expect(steps.length).toBe(3);
    expect(steps[0].value).toBeGreaterThan(0);
    expect(steps[2].value).toBe(0);
  });

  it('should render range pills in template', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const pills = compiled.querySelectorAll('.range-pill');
    expect(pills.length).toBe(4);
    expect(pills[0].textContent?.trim()).toBe('3M');
    expect(pills[1].textContent?.trim()).toBe('6M');
    expect(pills[2].textContent?.trim()).toBe('9M');
    expect(pills[3].textContent?.trim()).toBe('1Y');
  });
});
