import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { COMPOSITION_BUFFER_MODE, FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { VehicleFilterParams, VehicleStatus } from '../../../../core/models/vehicle.model';

@Component({
  selector: 'iid-vehicle-filter-bar',
  standalone: true,
  imports: [FormsModule],
  providers: [
    { provide: COMPOSITION_BUFFER_MODE, useValue: false }
  ],
  templateUrl: './vehicle-filter-bar.component.html',
  styleUrl: './vehicle-filter-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VehicleFilterBarComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  private readonly makeInput$ = new Subject<string>();
  private readonly modelInput$ = new Subject<string>();

  readonly filterChange = output<VehicleFilterParams>();

  readonly searchMake = signal<string>('');
  readonly searchModel = signal<string>('');
  readonly selectedStatus = signal<VehicleStatus | ''>('');
  readonly minAgeDays = signal<number | null>(null);
  readonly sortBy = signal<string>('createdAt');
  readonly sortOrder = signal<'asc' | 'desc'>('desc');

  private static readonly TEXT_INPUT_DEBOUNCE_MS = 250;

  ngOnInit(): void {
    this.makeInput$
      .pipe(
        debounceTime(VehicleFilterBarComponent.TEXT_INPUT_DEBOUNCE_MS),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => this.applyFilters());

    this.modelInput$
      .pipe(
        debounceTime(VehicleFilterBarComponent.TEXT_INPUT_DEBOUNCE_MS),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => this.applyFilters());
  }

  onMakeInput(value: string): void {
    this.searchMake.set(value);
    this.makeInput$.next(value);
  }

  onModelInput(value: string): void {
    this.searchModel.set(value);
    this.modelInput$.next(value);
  }

  applyFilters(): void {
    const params: VehicleFilterParams = {
      make: this.searchMake().trim() || undefined,
      model: this.searchModel().trim() || undefined,
      status: this.selectedStatus() ? (this.selectedStatus() as VehicleStatus) : undefined,
      minAgeDays: this.minAgeDays() !== null ? this.minAgeDays()! : undefined,
      sort: this.sortBy(),
      order: this.sortOrder()
    };
    this.filterChange.emit(params);
  }

  reset(): void {
    this.searchMake.set('');
    this.searchModel.set('');
    this.selectedStatus.set('');
    this.minAgeDays.set(null);
    this.sortBy.set('createdAt');
    this.sortOrder.set('desc');
    // Flush any pending debounced text input so the reset takes effect immediately
    this.makeInput$.next('');
    this.modelInput$.next('');
    this.applyFilters();
  }
}
