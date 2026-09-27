import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, debounceTime, tap } from 'rxjs';
import { CreateDealershipRequest, Dealership, UpdateDealershipRequest } from '../models/dealership.model';
import { RealtimeDealershipResponse } from '../models/realtime.model';
import { ApiConfigService } from './api.service';
import { RealtimeService } from './realtime.service';

@Injectable({
  providedIn: 'root'
})
export class DealershipService {
  private readonly http = inject(HttpClient);
  private readonly apiConfig = inject(ApiConfigService);
  private readonly realtime = inject(RealtimeService);
  private readonly destroyRef = inject(DestroyRef);

  private static readonly STORAGE_KEY = 'iid_selected_dealership_id';

  readonly dealerships = signal<readonly Dealership[]>([]);
  readonly selectedDealership = signal<Dealership | null>(null);
  readonly selectedDealershipId = computed(() => this.selectedDealership()?.id ?? null);
  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  // Transient signal indicating which dealership was just added or updated over SignalR
  readonly recentlyUpdatedDealershipId = signal<string | null>(null);
  private highlightTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    this.loadDealerships();
    this.setupRealtimeListeners();
  }

  private setupRealtimeListeners(): void {
    // Listen for new dealership additions pushed by backend SignalR
    this.realtime.dealershipAdded$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(d => this.handleDealershipAdded(d));

    // Listen for dealership updates pushed by backend SignalR
    this.realtime.dealershipUpdated$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(d => this.handleDealershipUpdated(d));

    // Silently refresh dealership inventory counts when inventory changes
    this.realtime.inventoryChanged$
      .pipe(
        debounceTime(400),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.loadDealerships(false);
      });
  }

  private handleDealershipAdded(d: RealtimeDealershipResponse): void {
    this.dealerships.update(list => {
      const idx = list.findIndex(item => item.id === d.id);
      if (idx >= 0) {
        const copy = [...list];
        copy[idx] = {
          ...copy[idx],
          name: d.name,
          code: d.code,
          city: d.city,
          state: d.state,
          phone: d.phone
        };
        return copy;
      }

      const newDealership: Dealership = {
        id: d.id,
        name: d.name,
        code: d.code,
        city: d.city,
        state: d.state,
        phone: d.phone,
        vehicleCount: 0
      };
      return [...list, newDealership];
    });

    if (!this.selectedDealership()) {
      const found = this.dealerships().find(item => item.id === d.id);
      if (found) {
        this.applySelection(found);
      }
    }

    this.triggerHighlight(d.id);
    this.loadDealerships(false);
  }

  private handleDealershipUpdated(d: RealtimeDealershipResponse): void {
    this.dealerships.update(list =>
      list.map(item =>
        item.id === d.id
          ? {
              ...item,
              name: d.name,
              code: d.code,
              city: d.city,
              state: d.state,
              phone: d.phone
            }
          : item
      )
    );

    const currentSelected = this.selectedDealership();
    if (currentSelected?.id === d.id) {
      this.selectedDealership.set({
        ...currentSelected,
        name: d.name,
        code: d.code,
        city: d.city,
        state: d.state,
        phone: d.phone
      });
    }

    this.triggerHighlight(d.id);
    this.loadDealerships(false);
  }

  private triggerHighlight(id: string): void {
    if (this.highlightTimer) {
      clearTimeout(this.highlightTimer);
    }
    this.recentlyUpdatedDealershipId.set(id);
    this.highlightTimer = setTimeout(() => {
      if (this.recentlyUpdatedDealershipId() === id) {
        this.recentlyUpdatedDealershipId.set(null);
      }
      this.highlightTimer = null;
    }, 3500);
  }

  loadDealerships(showSpinner = true): void {
    if (showSpinner) {
      this.isLoading.set(true);
    }
    this.error.set(null);

    const url = this.apiConfig.getApiUrl('/api/v1/dealerships');
    this.http.get<{ data: Dealership[] }>(url).subscribe({
      next: res => {
        const list = res.data ?? [];
        this.dealerships.set(list);
        if (showSpinner) {
          this.isLoading.set(false);
        }

        if (list.length > 0) {
          const currentSelected = this.selectedDealership();
          const savedId = localStorage.getItem(DealershipService.STORAGE_KEY);
          // If we already have a selection that still exists in list, keep it
          const found = list.find(d => d.id === (currentSelected?.id ?? savedId));
          const target = found ?? list[0];
          this.applySelection(target);
        }
      },
      error: err => {
        if (showSpinner) {
          this.isLoading.set(false);
        }
        this.error.set(err?.message ?? 'Failed to load dealerships');
      }
    });
  }

  createDealership(request: CreateDealershipRequest): Observable<{ id: string }> {
    const url = this.apiConfig.getApiUrl('/api/v1/dealerships');
    return this.http.post<{ id: string }>(url, request).pipe(
      tap(() => this.loadDealerships(false))
    );
  }

  updateDealership(id: string, request: UpdateDealershipRequest): Observable<{ data: { id: string } }> {
    const url = this.apiConfig.getApiUrl(`/api/v1/dealerships/${id}`);
    return this.http.put<{ data: { id: string } }>(url, request).pipe(
      tap(() => this.loadDealerships(false))
    );
  }

  selectDealership(dealershipOrId: Dealership | string): void {
    const list = this.dealerships();
    const target =
      typeof dealershipOrId === 'string'
        ? list.find(d => d.id === dealershipOrId) ?? null
        : dealershipOrId;

    if (target) {
      this.applySelection(target);
    }
  }

  private applySelection(target: Dealership): void {
    const prevId = this.selectedDealership()?.id ?? null;
    this.selectedDealership.set(target);
    localStorage.setItem(DealershipService.STORAGE_KEY, target.id);

    if (prevId !== target.id) {
      this.realtime.switchDealership(target.id, prevId);
    }
  }
}

