type NavigateFn = (to: string, options?: { replace?: boolean }) => unknown;

let navigateFn: NavigateFn | null = null;

/** Registered once by the app router so non-component code (e.g. logout) can navigate. */
export function setNavigator(fn: NavigateFn): void {
  navigateFn = fn;
}

export function navigateTo(to: string): void {
  if (navigateFn) {
    void navigateFn(to);
  } else if (typeof window !== 'undefined') {
    window.location.assign(to);
  }
}
