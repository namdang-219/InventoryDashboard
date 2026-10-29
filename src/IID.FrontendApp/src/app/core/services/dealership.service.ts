import { useEffect } from 'react';
import { CreateDealershipRequest, Dealership, UpdateDealershipRequest } from '../models/dealership.model';
import { RealtimeDealershipResponse } from '../models/realtime.model';
import {
  dealershipAddedFromRealtime,
  dealershipUpdatedFromRealtime,
  dealershipsError,
  dealershipsLoading,
  dealershipsSet,
  recentlyUpdatedDealershipSet,
  selectedDealershipSet
} from '../store/dealership.slice';
import { useAppSelector } from '../store/hooks';
import { store } from '../store/store';
import { apiConfig, get, post, put } from './api.service';
import { realtimeEvents, realtimeService } from './realtime.service';

const STORAGE_KEY = 'iid_selected_dealership_id';
const dispatch = store.dispatch;
const state = () => store.getState().dealership;

let highlightTimer: ReturnType<typeof setTimeout> | null = null;
let inventoryChangedTimer: ReturnType<typeof setTimeout> | null = null;
let started = false;

function handleDealershipAdded(d: RealtimeDealershipResponse): void {
  dispatch(dealershipAddedFromRealtime(d));

  if (!state().selectedDealership) {
    const found = state().dealerships.find(item => item.id === d.id);
    if (found) {
      applySelection(found);
    }
  }

  triggerHighlight(d.id);
  dealershipService.loadDealerships(false);
}

function handleDealershipUpdated(d: RealtimeDealershipResponse): void {
  dispatch(dealershipUpdatedFromRealtime(d));
  triggerHighlight(d.id);
  dealershipService.loadDealerships(false);
}

function triggerHighlight(id: string): void {
  if (highlightTimer) {
    clearTimeout(highlightTimer);
  }
  dispatch(recentlyUpdatedDealershipSet(id));
  highlightTimer = setTimeout(() => {
    if (state().recentlyUpdatedDealershipId === id) {
      dispatch(recentlyUpdatedDealershipSet(null));
    }
    highlightTimer = null;
  }, 3500);
}

function applySelection(target: Dealership): void {
  const prevId = state().selectedDealership?.id ?? null;
  dispatch(selectedDealershipSet(target));
  localStorage.setItem(STORAGE_KEY, target.id);

  if (prevId !== target.id) {
    void realtimeService.switchDealership(target.id, prevId);
  }
}

export const dealershipService = {
  /** Loads dealerships and wires realtime listeners (once). Mirrors the Angular service constructor. */
  start(): void {
    if (started) return;
    started = true;
    dealershipService.loadDealerships();

    // Listen for new dealership additions pushed by backend SignalR
    realtimeEvents.on('dealershipAdded', handleDealershipAdded);
    // Listen for dealership updates pushed by backend SignalR
    realtimeEvents.on('dealershipUpdated', handleDealershipUpdated);
    // Silently refresh dealership inventory counts when inventory changes (debounced 400ms)
    realtimeEvents.on('inventoryChanged', () => {
      if (inventoryChangedTimer) clearTimeout(inventoryChangedTimer);
      inventoryChangedTimer = setTimeout(() => {
        inventoryChangedTimer = null;
        dealershipService.loadDealerships(false);
      }, 400);
    });
  },

  loadDealerships(showSpinner = true): void {
    if (showSpinner) {
      dispatch(dealershipsLoading(true));
    }
    dispatch(dealershipsError(null));

    get<{ data: Dealership[] }>(apiConfig.getApiUrl('/api/v1/dealerships'))
      .then(res => {
        const list = res.data ?? [];
        dispatch(dealershipsSet(list));
        if (showSpinner) {
          dispatch(dealershipsLoading(false));
        }

        if (list.length > 0) {
          const currentSelected = state().selectedDealership;
          const savedId = localStorage.getItem(STORAGE_KEY);
          // If we already have a selection that still exists in list, keep it
          const found = list.find(d => d.id === (currentSelected?.id ?? savedId));
          const target = found ?? list[0];
          applySelection(target);
        }
      })
      .catch(err => {
        if (showSpinner) {
          dispatch(dealershipsLoading(false));
        }
        dispatch(dealershipsError(err?.message ?? 'Failed to load dealerships'));
      });
  },

  async createDealership(request: CreateDealershipRequest): Promise<{ id: string }> {
    const res = await post<{ id: string }>(apiConfig.getApiUrl('/api/v1/dealerships'), request);
    dealershipService.loadDealerships(false);
    return res;
  },

  async updateDealership(id: string, request: UpdateDealershipRequest): Promise<{ data: { id: string } }> {
    const res = await put<{ data: { id: string } }>(apiConfig.getApiUrl(`/api/v1/dealerships/${id}`), request);
    dealershipService.loadDealerships(false);
    return res;
  },

  selectDealership(dealershipOrId: Dealership | string): void {
    const list = state().dealerships;
    const target =
      typeof dealershipOrId === 'string' ? (list.find(d => d.id === dealershipOrId) ?? null) : dealershipOrId;

    if (target) {
      applySelection(target);
    }
  }
};

export function useDealerships() {
  const s = useAppSelector(st => st.dealership);

  useEffect(() => {
    dealershipService.start();
  }, []);

  return { ...s, selectedDealershipId: s.selectedDealership?.id ?? null, ...dealershipService };
}
