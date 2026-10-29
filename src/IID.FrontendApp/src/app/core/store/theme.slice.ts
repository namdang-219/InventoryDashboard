import { PayloadAction, createSlice } from '@reduxjs/toolkit';

export type ThemeMode = 'light' | 'dark';

const STORAGE_KEY = 'iid_theme_mode';

function readInitialTheme(): ThemeMode {
  // Default is explicitly 'light'
  const saved = typeof localStorage !== 'undefined' ? (localStorage.getItem(STORAGE_KEY) as ThemeMode | null) : null;
  return saved === 'dark' ? 'dark' : 'light';
}

const themeSlice = createSlice({
  name: 'theme',
  initialState: { currentTheme: readInitialTheme() as ThemeMode },
  reducers: {
    themeSet(state, action: PayloadAction<ThemeMode>) {
      state.currentTheme = action.payload;
    }
  }
});

export const { themeSet } = themeSlice.actions;
export const themeReducer = themeSlice.reducer;
export const THEME_STORAGE_KEY = STORAGE_KEY;
