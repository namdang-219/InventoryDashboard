import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { Dealership } from '../../../../core/models/dealership.model';
import { AuthService } from '../../../../core/services/auth.service';
import { DealershipService } from '../../../../core/services/dealership.service';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { DealershipFormModalComponent } from '../../components/dealership-form-modal/dealership-form-modal.component';

@Component({
  selector: 'iid-dealership-list-page',
  standalone: true,
  imports: [
    FormsModule,
    DealershipFormModalComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent
  ],
  templateUrl: './dealership-list-page.component.html',
  styleUrl: './dealership-list-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DealershipListPageComponent {
  readonly dealershipService = inject(DealershipService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly isManager = this.auth.isManager;

  readonly searchInputText = signal<string>('');
  readonly debouncedQuery = signal<string>('');
  private readonly searchInput$ = new Subject<string>();

  private static readonly SEARCH_DEBOUNCE_MS = 250;

  constructor() {
    this.searchInput$
      .pipe(
        debounceTime(DealershipListPageComponent.SEARCH_DEBOUNCE_MS),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(query => {
        this.debouncedQuery.set(query);
      });
  }

  onSearchInput(value: string): void {
    this.searchInputText.set(value);
    this.searchInput$.next(value);
  }

  flushSearch(): void {
    this.debouncedQuery.set(this.searchInputText());
  }

  clearSearch(): void {
    this.searchInputText.set('');
    this.debouncedQuery.set('');
    this.searchInput$.next('');
  }

  // Modal state
  readonly showModal = signal<boolean>(false);
  readonly selectedForEdit = signal<Dealership | null>(null);

  // Filtered list
  readonly filteredDealerships = computed(() => {
    const list = this.dealershipService.dealerships();
    const query = this.debouncedQuery().trim().toLowerCase();
    if (!query) return list;

    return list.filter(d =>
      d.name.toLowerCase().includes(query) ||
      d.code.toLowerCase().includes(query) ||
      d.city.toLowerCase().includes(query) ||
      d.state.toLowerCase().includes(query) ||
      (d.phone && d.phone.toLowerCase().includes(query))
    );
  });

  // Summary Metrics
  readonly totalShowrooms = computed(() => this.dealershipService.dealerships().length);

  readonly totalVehicles = computed(() => {
    return this.dealershipService.dealerships().reduce((sum, d) => sum + (d.vehicleCount ?? 0), 0);
  });

  readonly totalStates = computed(() => {
    const states = new Set(this.dealershipService.dealerships().map(d => d.state));
    return states.size;
  });

  openCreateModal(): void {
    this.selectedForEdit.set(null);
    this.showModal.set(true);
  }

  openEditModal(dealership: Dealership): void {
    this.selectedForEdit.set(dealership);
    this.showModal.set(true);
  }

  selectDealership(d: Dealership): void {
    this.dealershipService.selectDealership(d);
  }

  viewDealershipInventory(d: Dealership): void {
    this.dealershipService.selectDealership(d);
    this.router.navigate(['/vehicles']);
  }

  onModalSaved(): void {
    this.dealershipService.loadDealerships();
  }
}
