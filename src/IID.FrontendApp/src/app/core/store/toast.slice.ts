import { PayloadAction, createSlice } from '@reduxjs/toolkit';

export interface ToastMessage {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  message: string;
  timestamp: Date;
  durationMs?: number;
}

interface ToastState {
  toasts: readonly ToastMessage[];
}

const initialState: ToastState = { toasts: [] };

const toastSlice = createSlice({
  name: 'toast',
  initialState,
  reducers: {
    toastAdded(state, action: PayloadAction<ToastMessage>) {
      state.toasts = [action.payload, ...state.toasts];
    },
    toastDismissed(state, action: PayloadAction<string>) {
      state.toasts = state.toasts.filter(t => t.id !== action.payload);
    },
    toastsCleared(state) {
      state.toasts = [];
    }
  }
});

export const { toastAdded, toastDismissed, toastsCleared } = toastSlice.actions;
export const toastReducer = toastSlice.reducer;
