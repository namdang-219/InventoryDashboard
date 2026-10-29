import { PayloadAction, createSlice } from '@reduxjs/toolkit';
import { Dealership } from '../models/dealership.model';
import { RealtimeDealershipResponse } from '../models/realtime.model';

interface DealershipState {
  dealerships: readonly Dealership[];
  selectedDealership: Dealership | null;
  isLoading: boolean;
  error: string | null;
  recentlyUpdatedDealershipId: string | null;
}

const initialState: DealershipState = {
  dealerships: [],
  selectedDealership: null,
  isLoading: false,
  error: null,
  recentlyUpdatedDealershipId: null
};

function patchDealership(item: Dealership, d: RealtimeDealershipResponse): Dealership {
  return {
    ...item,
    name: d.name,
    code: d.code,
    city: d.city,
    state: d.state,
    phone: d.phone
  };
}

const dealershipSlice = createSlice({
  name: 'dealership',
  initialState,
  reducers: {
    dealershipsLoading(state, action: PayloadAction<boolean>) {
      state.isLoading = action.payload;
    },
    dealershipsError(state, action: PayloadAction<string | null>) {
      state.error = action.payload;
    },
    dealershipsSet(state, action: PayloadAction<readonly Dealership[]>) {
      state.dealerships = [...action.payload];
    },
    dealershipAddedFromRealtime(state, action: PayloadAction<RealtimeDealershipResponse>) {
      const d = action.payload;
      const idx = state.dealerships.findIndex(item => item.id === d.id);
      if (idx >= 0) {
        const copy = [...state.dealerships];
        copy[idx] = patchDealership(copy[idx], d);
        state.dealerships = copy;
      } else {
        const newDealership: Dealership = {
          id: d.id,
          name: d.name,
          code: d.code,
          city: d.city,
          state: d.state,
          phone: d.phone,
          vehicleCount: 0
        };
        state.dealerships = [...state.dealerships, newDealership];
      }
    },
    dealershipUpdatedFromRealtime(state, action: PayloadAction<RealtimeDealershipResponse>) {
      const d = action.payload;
      state.dealerships = state.dealerships.map(item =>
        item.id === d.id ? patchDealership(item, d) : item
      );
      if (state.selectedDealership?.id === d.id) {
        state.selectedDealership = patchDealership(state.selectedDealership, d);
      }
    },
    selectedDealershipSet(state, action: PayloadAction<Dealership | null>) {
      state.selectedDealership = action.payload;
    },
    recentlyUpdatedDealershipSet(state, action: PayloadAction<string | null>) {
      state.recentlyUpdatedDealershipId = action.payload;
    }
  }
});

export const {
  dealershipsLoading,
  dealershipsError,
  dealershipsSet,
  dealershipAddedFromRealtime,
  dealershipUpdatedFromRealtime,
  selectedDealershipSet,
  recentlyUpdatedDealershipSet
} = dealershipSlice.actions;
export const dealershipReducer = dealershipSlice.reducer;
