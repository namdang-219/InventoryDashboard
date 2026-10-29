import { describe, expect, it } from 'vitest';
import { LiveActivityFeedItem } from '../models/realtime.model';
import {
  activitiesLoaded,
  activityAdded,
  activityMarkedRead,
  activityTicked,
  allActivitiesMarkedRead,
  moreActivitiesLoaded,
  realtimeReducer
} from './realtime.slice';

function makeActivity(overrides: Partial<LiveActivityFeedItem> = {}): LiveActivityFeedItem {
  return {
    id: overrides.id ?? 'act-1',
    type: overrides.type ?? 'action',
    title: overrides.title ?? 'Action: Price Reduction',
    detail: overrides.detail ?? 'Reduced by $500',
    timestamp: overrides.timestamp ?? new Date('2026-06-15T10:00:00Z'),
    severity: overrides.severity ?? 'info',
    isRead: overrides.isRead ?? false,
    ...overrides
  };
}

describe('realtimeSlice', () => {
  it('loads initial activities via activitiesLoaded', () => {
    const items = [
      makeActivity({ id: 'a1', isRead: false }),
      makeActivity({ id: 'a2', isRead: true })
    ];

    const state = realtimeReducer(
      undefined,
      activitiesLoaded({
        items,
        nextCursor: 'cursor-1',
        hasMore: true,
        total: 12,
        unreadCount: 4
      })
    );

    expect(state.recentActivities).toEqual(items);
    expect(state.nextCursor).toBe('cursor-1');
    expect(state.hasMoreActivities).toBe(true);
    expect(state.totalActivities).toBe(12);
    expect(state.unreadCount).toBe(4);
  });

  it('appends and deduplicates activities via moreActivitiesLoaded', () => {
    const initial = realtimeReducer(
      undefined,
      activitiesLoaded({
        items: [makeActivity({ id: 'a1' }), makeActivity({ id: 'a2' })],
        nextCursor: 'cursor-1',
        hasMore: true,
        total: 3,
        unreadCount: 2
      })
    );

    const next = realtimeReducer(
      initial,
      moreActivitiesLoaded({
        items: [makeActivity({ id: 'a2', title: 'Duplicate' }), makeActivity({ id: 'a3' })],
        nextCursor: null,
        hasMore: false
      })
    );

    expect(next.recentActivities.map(a => a.id)).toEqual(['a1', 'a2', 'a3']);
    expect(next.recentActivities[1].title).toBe('Action: Price Reduction');
    expect(next.nextCursor).toBeNull();
    expect(next.hasMoreActivities).toBe(false);
    // Preserves total and unreadCount when omitted
    expect(next.totalActivities).toBe(3);
    expect(next.unreadCount).toBe(2);
  });

  it('prepends unread activity via activityAdded, incrementing totalActivities and unreadCount, and deduplicating existing ids', () => {
    const base = realtimeReducer(
      undefined,
      activitiesLoaded({
        items: [makeActivity({ id: 'a1', isRead: true })],
        nextCursor: null,
        hasMore: false,
        total: 1,
        unreadCount: 0
      })
    );

    const withUnread = realtimeReducer(base, activityAdded(makeActivity({ id: 'a2', isRead: false })));
    expect(withUnread.recentActivities.map(a => a.id)).toEqual(['a2', 'a1']);
    expect(withUnread.totalActivities).toBe(2);
    expect(withUnread.unreadCount).toBe(1);

    // Duplicate id should be ignored completely
    const afterDuplicate = realtimeReducer(
      withUnread,
      activityAdded(makeActivity({ id: 'a2', isRead: false }))
    );
    expect(afterDuplicate.recentActivities).toHaveLength(2);
    expect(afterDuplicate.totalActivities).toBe(2);
    expect(afterDuplicate.unreadCount).toBe(1);

    // Already-read activity increments totalActivities but not unreadCount
    const withRead = realtimeReducer(
      afterDuplicate,
      activityAdded(makeActivity({ id: 'a3', isRead: true }))
    );
    expect(withRead.recentActivities.map(a => a.id)).toEqual(['a3', 'a2', 'a1']);
    expect(withRead.totalActivities).toBe(3);
    expect(withRead.unreadCount).toBe(1);
  });

  it('marks a single activity read via activityMarkedRead and decrements unreadCount once', () => {
    const loaded = realtimeReducer(
      undefined,
      activitiesLoaded({
        items: [
          makeActivity({ id: 'a1', isRead: false }),
          makeActivity({ id: 'a2', isRead: false })
        ],
        nextCursor: null,
        hasMore: false,
        total: 2,
        unreadCount: 2
      })
    );

    const afterFirstMark = realtimeReducer(loaded, activityMarkedRead('a1'));
    expect(afterFirstMark.recentActivities.find(a => a.id === 'a1')?.isRead).toBe(true);
    expect(afterFirstMark.recentActivities.find(a => a.id === 'a2')?.isRead).toBe(false);
    expect(afterFirstMark.unreadCount).toBe(1);

    // Marking an already-read activity again does not decrement unreadCount further
    const afterSecondMark = realtimeReducer(afterFirstMark, activityMarkedRead('a1'));
    expect(afterSecondMark.unreadCount).toBe(1);
  });

  it('marks all activities read and resets unreadCount via allActivitiesMarkedRead', () => {
    const loaded = realtimeReducer(
      undefined,
      activitiesLoaded({
        items: [
          makeActivity({ id: 'a1', isRead: false }),
          makeActivity({ id: 'a2', isRead: false })
        ],
        nextCursor: null,
        hasMore: false,
        total: 2,
        unreadCount: 5
      })
    );

    const cleared = realtimeReducer(loaded, allActivitiesMarkedRead());
    expect(cleared.unreadCount).toBe(0);
    expect(cleared.recentActivities.every(a => a.isRead === true)).toBe(true);
  });

  it('increments latestActivityTick via activityTicked', () => {
    const s0 = realtimeReducer(undefined, { type: '@@INIT' });
    expect(s0.latestActivityTick).toBe(0);

    const s1 = realtimeReducer(s0, activityTicked());
    expect(s1.latestActivityTick).toBe(1);

    const s2 = realtimeReducer(s1, activityTicked());
    expect(s2.latestActivityTick).toBe(2);
  });
});
