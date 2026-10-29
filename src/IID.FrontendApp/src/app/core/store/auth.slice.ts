import { PayloadAction, createSlice } from '@reduxjs/toolkit';
import { UserDto } from '../models/auth.model';

export const TOKEN_KEY = 'iid_access_token';
export const REFRESH_TOKEN_KEY = 'iid_refresh_token';
export const USER_KEY = 'iid_user';

function getStoredUser(): UserDto | null {
  try {
    const saved = localStorage.getItem(USER_KEY);
    return saved ? (JSON.parse(saved) as UserDto) : null;
  } catch {
    return null;
  }
}

interface AuthState {
  currentUser: UserDto | null;
  token: string | null;
}

const initialState: AuthState = {
  currentUser: getStoredUser(),
  token: localStorage.getItem(TOKEN_KEY)
};

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    authSucceeded(state, action: PayloadAction<{ token: string; user: UserDto }>) {
      state.token = action.payload.token;
      state.currentUser = {
        ...action.payload.user,
        roles: [...action.payload.user.roles]
      };
    },
    authCleared(state) {
      state.token = null;
      state.currentUser = null;
    }
  }
});

export const { authSucceeded, authCleared } = authSlice.actions;
export const authReducer = authSlice.reducer;
