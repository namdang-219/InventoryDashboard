import { Injectable, computed, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly STORAGE_KEY = 'iid_theme_mode';

  readonly currentTheme = signal<ThemeMode>('light');
  readonly isDark = computed(() => this.currentTheme() === 'dark');

  constructor() {
    // Default is explicitly 'light' as requested
    const saved = typeof localStorage !== 'undefined' ? (localStorage.getItem(this.STORAGE_KEY) as ThemeMode | null) : null;
    const initialTheme: ThemeMode = saved === 'dark' ? 'dark' : 'light';
    this.setTheme(initialTheme);
  }

  toggleTheme(): void {
    const next = this.currentTheme() === 'dark' ? 'light' : 'dark';
    this.setTheme(next);
  }

  setTheme(theme: ThemeMode): void {
    this.currentTheme.set(theme);
    if (typeof localStorage !== 'undefined') {
      try {
        localStorage.setItem(this.STORAGE_KEY, theme);
      } catch {
        // Safe fallback
      }
    }
    if (typeof document !== 'undefined') {
      document.documentElement.setAttribute('data-theme', theme);
    }
  }
}
