import { PayloadAction, createSlice } from '@reduxjs/toolkit';
import { HubConnectionStatus, LiveActivityFeedItem } from '../models/realtime.model';

interface RealtimeState {
  connectionStatus: HubConnectionStatus;
  activeDealershipId: string | null;
  recentActivities: readonly LiveActivityFeedItem[];
  nextCursor: string | null;
  hasMoreActivities: boolean;
  isLoadingActivities: boolean;
  isLoadingMoreActivities: boolean;
  totalActivities: number;
  unreadCount: number;
  /** Monotonically increasing tick that increments whenever a new realtime activity arrives. */
  latestActivityTick: number;
}

const initialState: RealtimeState = {
  connectionStatus: 'disconnected',
  activeDealershipId: null,
  recentActivities: [],
  nextCursor: null,
  hasMoreActivities: false,
  isLoadingActivities: false,
  isLoadingMoreActivities: false,
  totalActivities: 0,
  unreadCount: 0,
  latestActivityTick: 0
};

const realtimeSlice = createSlice({
  name: 'realtime',
  initialState,
  reducers: {
    connectionStatusSet(state, action: PayloadAction<HubConnectionStatus>) {
      state.connectionStatus = action.payload;
    },
    activeDealershipSet(state, action: PayloadAction<string | null>) {
      state.activeDealershipId = action.payload;
    },
    activitiesLoading(state, action: PayloadAction<boolean>) {
      state.isLoadingActivities = action.payload;
    },
    moreActivitiesLoading(state, action: PayloadAction<boolean>) {
      state.isLoadingMoreActivities = action.payload;
    },
    activitiesLoaded(
      state,
      action: PayloadAction<{
        items: readonly LiveActivityFeedItem[];
        nextCursor: string | null;
        hasMore: boolean;
        total: number;
        unreadCount: number;
      }>
    ) {
      state.recentActivities = [...action.payload.items];
      state.nextCursor = action.payload.nextCursor;
      state.hasMoreActivities = action.payload.hasMore;
      state.totalActivities = action.payload.total;
      state.unreadCount = action.payload.unreadCount;
    },
    moreActivitiesLoaded(
      state,
      action: PayloadAction<{
        items: readonly LiveActivityFeedItem[];
        nextCursor: string | null;
        hasMore: boolean;
        total?: number;
        unreadCount?: number;
      }>
    ) {
      const existingIds = new Set(state.recentActivities.map(a => a.id));
      const newItems = action.payload.items.filter(item => !existingIds.has(item.id));
      state.recentActivities = [...state.recentActivities, ...newItems];
      state.nextCursor = action.payload.nextCursor;
      state.hasMoreActivities = action.payload.hasMore;
      if (action.payload.total !== undefined) state.totalActivities = action.payload.total;
      if (action.payload.unreadCount !== undefined) state.unreadCount = action.payload.unreadCount;
    },
    activityAdded(state, action: PayloadAction<LiveActivityFeedItem>) {
      if (state.recentActivities.some(existing => existing.id === action.payload.id)) return;
      state.recentActivities = [action.payload, ...state.recentActivities];
      state.totalActivities += 1;
      if (!action.payload.isRead) state.unreadCount += 1;
    },
    activityMarkedRead(state, action: PayloadAction<string>) {
      const target = state.recentActivities.find(item => item.id === action.payload);
      if (target && !target.isRead) state.unreadCount = Math.max(0, state.unreadCount - 1);
      state.recentActivities = state.recentActivities.map(item =>
        item.id === action.payload ? { ...item, isRead: true } : item
      );
    },
    allActivitiesMarkedRead(state) {
      state.unreadCount = 0;
      state.recentActivities = state.recentActivities.map(item => ({ ...item, isRead: true }));
    },
    activityTicked(state) {
      state.latestActivityTick += 1;
    }
  }
});

export const {
  connectionStatusSet,
  activeDealershipSet,
  activitiesLoading,
  moreActivitiesLoading,
  activitiesLoaded,
  moreActivitiesLoaded,
  activityAdded,
  activityMarkedRead,
  allActivitiesMarkedRead,
  activityTicked
} = realtimeSlice.actions;
export const realtimeReducer = realtimeSlice.reducer;
