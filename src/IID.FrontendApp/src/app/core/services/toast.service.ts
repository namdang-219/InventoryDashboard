import { useMemo } from 'react';
import { useAppSelector } from '../store/hooks';
import { store } from '../store/store';
import { ToastMessage, toastAdded, toastDismissed, toastsCleared } from '../store/toast.slice';

export type { ToastMessage };

export const toastService = {
  show(type: ToastMessage['type'], title: string, message: string, durationMs = 4500): void {
    const id = Math.random().toString(36).substring(2, 9);
    const toast: ToastMessage = { id, type, title, message, timestamp: new Date(), durationMs };

    store.dispatch(toastAdded(toast));

    if (durationMs > 0) {
      setTimeout(() => {
        toastService.dismiss(id);
      }, durationMs);
    }
  },

  success(title: string, message: string): void {
    toastService.show('success', title, message);
  },

  error(title: string, message: string): void {
    toastService.show('error', title, message, 6000);
  },

  warning(title: string, message: string): void {
    toastService.show('warning', title, message);
  },

  info(title: string, message: string): void {
    toastService.show('info', title, message);
  },

  dismiss(id: string): void {
    store.dispatch(toastDismissed(id));
  },

  clear(): void {
    store.dispatch(toastsCleared());
  }
};

export function useToast() {
  const toasts = useAppSelector(s => s.toast.toasts);
  return useMemo(() => ({ ...toastService, toasts }), [toasts]);
}
