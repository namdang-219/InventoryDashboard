import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { CreateDealershipRequest, Dealership, UpdateDealershipRequest } from '../models/dealership.model';
import { ApiConfigService } from './api.service';
import { RealtimeService } from './realtime.service';

@Injectable({
  providedIn: 'root'
})
export class DealershipService {
  private readonly http = inject(HttpClient);
  private readonly apiConfig = inject(ApiConfigService);
  private readonly realtime = inject(RealtimeService);

  private static readonly STORAGE_KEY = 'iid_selected_dealership_id';

  readonly dealerships = signal<readonly Dealership[]>([]);
  readonly selectedDealership = signal<Dealership | null>(null);
  readonly selectedDealershipId = computed(() => this.selectedDealership()?.id ?? null);
  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  constructor() {
    this.loadDealerships();
  }

  loadDealerships(): void {
    this.isLoading.set(true);
    this.error.set(null);

    const url = this.apiConfig.getApiUrl('/api/v1/dealerships');
    this.http.get<{ data: Dealership[] }>(url).subscribe({
      next: res => {
        const list = res.data ?? [];
        this.dealerships.set(list);
        this.isLoading.set(false);

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
        this.isLoading.set(false);
        this.error.set(err?.message ?? 'Failed to load dealerships');
      }
    });
  }

  createDealership(request: CreateDealershipRequest): Observable<{ id: string }> {
    const url = this.apiConfig.getApiUrl('/api/v1/dealerships');
    return this.http.post<{ id: string }>(url, request).pipe(
      tap(() => this.loadDealerships())
    );
  }

  updateDealership(id: string, request: UpdateDealershipRequest): Observable<{ data: { id: string } }> {
    const url = this.apiConfig.getApiUrl(`/api/v1/dealerships/${id}`);
    return this.http.put<{ data: { id: string } }>(url, request).pipe(
      tap(() => this.loadDealerships())
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
