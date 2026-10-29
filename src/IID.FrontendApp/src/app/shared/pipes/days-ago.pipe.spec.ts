import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { daysAgo } from './days-ago.pipe';

describe('daysAgo pipe', () => {
  const fixedNow = new Date(2026, 5, 15, 12, 0, 0); // Jun 15, 2026 12:00:00 local

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(fixedNow);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('returns em-dash for null, undefined, or empty string', () => {
    expect(daysAgo(null)).toBe('—');
    expect(daysAgo(undefined)).toBe('—');
    expect(daysAgo('')).toBe('—');
  });

  it('returns "Just now" when less than 60 seconds ago', () => {
    expect(daysAgo(fixedNow)).toBe('Just now');
    expect(daysAgo(new Date(fixedNow.getTime() - 30 * 1000))).toBe('Just now');
    expect(daysAgo(new Date(fixedNow.getTime() - 59 * 1000).toISOString())).toBe('Just now');
  });

  it('returns minutes ago when between 1 and 59 minutes ago', () => {
    expect(daysAgo(new Date(fixedNow.getTime() - 60 * 1000))).toBe('1m ago');
    expect(daysAgo(new Date(fixedNow.getTime() - 15 * 60 * 1000))).toBe('15m ago');
    expect(daysAgo(new Date(fixedNow.getTime() - 59 * 60 * 1000))).toBe('59m ago');
  });

  it('returns hours ago when between 1 and 23 hours ago', () => {
    expect(daysAgo(new Date(fixedNow.getTime() - 60 * 60 * 1000))).toBe('1h ago');
    expect(daysAgo(new Date(fixedNow.getTime() - 6 * 60 * 60 * 1000))).toBe('6h ago');
    expect(daysAgo(new Date(fixedNow.getTime() - 23 * 60 * 60 * 1000))).toBe('23h ago');
  });

  it('returns "Yesterday" when exactly 1 day ago', () => {
    expect(daysAgo(new Date(fixedNow.getTime() - 24 * 60 * 60 * 1000))).toBe('Yesterday');
    expect(daysAgo(new Date(fixedNow.getTime() - 36 * 60 * 60 * 1000))).toBe('Yesterday');
  });

  it('returns days ago when between 2 and 29 days ago', () => {
    expect(daysAgo(new Date(fixedNow.getTime() - 2 * 24 * 60 * 60 * 1000))).toBe('2d ago');
    expect(daysAgo(new Date(fixedNow.getTime() - 14 * 24 * 60 * 60 * 1000))).toBe('14d ago');
    expect(daysAgo(new Date(fixedNow.getTime() - 29 * 24 * 60 * 60 * 1000))).toBe('29d ago');
  });

  it('returns formatted date when 30 or more days ago', () => {
    const thirtyDaysAgo = new Date(2026, 4, 16, 12, 0, 0); // May 16, 2026 (30 days before Jun 15, 2026)
    expect(daysAgo(thirtyDaysAgo)).toBe('May 16, 2026');

    const olderDate = new Date(2025, 0, 5, 10, 0, 0); // Jan 5, 2025
    expect(daysAgo(olderDate)).toBe('Jan 5, 2025');
  });
});
