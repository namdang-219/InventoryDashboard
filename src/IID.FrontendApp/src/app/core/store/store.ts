import { configureStore } from '@reduxjs/toolkit';
import { authReducer } from './auth.slice';
import { dealershipReducer } from './dealership.slice';
import { realtimeReducer } from './realtime.slice';
import { themeReducer } from './theme.slice';
import { toastReducer } from './toast.slice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    toast: toastReducer,
    theme: themeReducer,
    dealership: dealershipReducer,
    realtime: realtimeReducer
  },
  middleware: getDefaultMiddleware =>
    // State holds Date objects (toasts, activity feed) and updater functions are
    // dispatched as payloads, so the serializability check is disabled.
    getDefaultMiddleware({ serializableCheck: false, immutableCheck: false })
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
