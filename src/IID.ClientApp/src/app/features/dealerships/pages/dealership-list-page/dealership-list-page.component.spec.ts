import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { DealershipListPageComponent } from './dealership-list-page.component';
import { DealershipService } from '../../../../core/services/dealership.service';
import { RealtimeService } from '../../../../core/services/realtime.service';
import { AuthService } from '../../../../core/services/auth.service';
import { Dealership } from '../../../../core/models/dealership.model';

describe('DealershipListPageComponent Realtime UI', () => {
  let component: DealershipListPageComponent;
  let fixture: ComponentFixture<DealershipListPageComponent>;
  let dealershipService: DealershipService;
  let realtime: RealtimeService;
  let httpTesting: HttpTestingController;

  const mockDealerships: Dealership[] = [
    {
      id: 'dealer-1',
      name: 'Austin Showroom',
      code: 'ATX-01',
      city: 'Austin',
      state: 'TX',
      phone: '512-555-0100',
      vehicleCount: 10
    },
    {
      id: 'dealer-2',
      name: 'Dallas Showroom',
      code: 'DAL-01',
      city: 'Dallas',
      state: 'TX',
      phone: '214-555-0200',
      vehicleCount: 20
    }
  ];

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [DealershipListPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        DealershipService,
        RealtimeService,
        AuthService
      ]
    }).compileComponents();

    httpTesting = TestBed.inject(HttpTestingController);
    dealershipService = TestBed.inject(DealershipService);
    realtime = TestBed.inject(RealtimeService);

    // Initial load from DealershipService constructor
    const req = httpTesting.expectOne(r => r.url.endsWith('/api/v1/dealerships'));
    req.flush({ data: mockDealerships });

    fixture = TestBed.createComponent(DealershipListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    httpTesting.verify();
    localStorage.clear();
  });

  it('should create and render dealership cards', () => {
    expect(component).toBeTruthy();
    expect(component.filteredDealerships().length).toBe(2);

    const compiled = fixture.nativeElement as HTMLElement;
    const cards = compiled.querySelectorAll('.dealer-card');
    expect(cards.length).toBe(2);
  });

  it('should display connected realtime badge in topbar', () => {
    realtime.connectionStatus.set('connected');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const pill = compiled.querySelector('.realtime-status-pill');
    expect(pill).toBeTruthy();
    expect(pill?.classList.contains('connected')).toBeTrue();
    expect(pill?.textContent).toContain('Live Network');
  });

  it('should display realtime badge and glow styling on card when dealership is updated via SignalR', () => {
    realtime.dealershipUpdated$.next({
      id: 'dealer-2',
      name: 'Dallas Flagship Center',
      code: 'DAL-99',
      city: 'Dallas',
      state: 'TX',
      phone: '214-555-9999'
    });

    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const cards = compiled.querySelectorAll('.dealer-card');
    const updatedCard = cards[1] as HTMLElement;

    expect(updatedCard.classList.contains('is-recently-updated')).toBeTrue();
    expect(updatedCard.querySelector('.realtime-badge')).toBeTruthy();
    expect(updatedCard.querySelector('.realtime-badge')?.textContent).toContain('Live Update');
    expect(updatedCard.querySelector('.dealer-name')?.textContent).toContain('Dallas Flagship Center');

    // Flush background sync
    const silentReq = httpTesting.expectOne(r => r.url.endsWith('/api/v1/dealerships'));
    silentReq.flush({
      data: [
        mockDealerships[0],
        { ...mockDealerships[1], name: 'Dallas Flagship Center', code: 'DAL-99' }
      ]
    });
  });

  it('should keep currently opened edit modal synced if realtime update arrives for that dealership', () => {
    component.openEditModal(mockDealerships[0]);
    expect(component.selectedForEdit()?.id).toBe('dealer-1');

    realtime.dealershipUpdated$.next({
      id: 'dealer-1',
      name: 'Austin Innovation Center',
      code: 'ATX-02',
      city: 'Austin',
      state: 'TX',
      phone: '512-555-0222'
    });

    // Modal target should be reactively updated
    expect(component.selectedForEdit()?.name).toBe('Austin Innovation Center');
    expect(component.selectedForEdit()?.code).toBe('ATX-02');

    // Flush background sync
    const silentReq = httpTesting.expectOne(r => r.url.endsWith('/api/v1/dealerships'));
    silentReq.flush({ data: mockDealerships });
  });
});
