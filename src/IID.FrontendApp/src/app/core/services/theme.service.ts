import { useMemo } from 'react';
import { useAppSelector } from '../store/hooks';
import { store } from '../store/store';
import { THEME_STORAGE_KEY, ThemeMode, themeSet } from '../store/theme.slice';

export type { ThemeMode };

export const themeService = {
  toggleTheme(): void {
    const next = store.getState().theme.currentTheme === 'dark' ? 'light' : 'dark';
    themeService.setTheme(next);
  },

  setTheme(theme: ThemeMode): void {
    store.dispatch(themeSet(theme));
    if (typeof localStorage !== 'undefined') {
      try {
        localStorage.setItem(THEME_STORAGE_KEY, theme);
      } catch {
        // Safe fallback
      }
    }
    if (typeof document !== 'undefined') {
      document.documentElement.setAttribute('data-theme', theme);
    }
  }
};

// Apply the persisted theme on startup (the Angular service did this in its constructor).
themeService.setTheme(store.getState().theme.currentTheme);

export function useTheme() {
  const currentTheme = useAppSelector(s => s.theme.currentTheme);
  return useMemo(
    () => ({ currentTheme, isDark: currentTheme === 'dark', ...themeService }),
    [currentTheme]
  );
}
