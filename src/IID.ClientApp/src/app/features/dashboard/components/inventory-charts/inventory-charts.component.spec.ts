import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InventoryChartsComponent } from './inventory-charts.component';
import { InventoryCharts } from '../../../../core/models/dashboard.model';

describe('InventoryChartsComponent', () => {
  let component: InventoryChartsComponent;
  let fixture: ComponentFixture<InventoryChartsComponent>;

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

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryChartsComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryChartsComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('charts', mockCharts);
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should compute max aging count and percentages', () => {
    expect(component.maxAgingCount()).toBe(6);
    expect(component.getAgingHeightPercent(6)).toBe(100);
  });

  it('should compute total fuel count and percentages', () => {
    expect(component.totalFuelCount()).toBe(12);
    expect(component.getFuelWidthPercent(6)).toBe(50);
  });

  it('should render 3 distribution chart cards', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const cards = compiled.querySelectorAll('.chart-card');
    expect(cards.length).toBe(3);
  });
});
