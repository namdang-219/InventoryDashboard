import { describe, expect, it } from 'vitest';
import { formatCurrency } from './format-currency.pipe';

describe('formatCurrency pipe', () => {
  it('returns em-dash for null, undefined, or NaN', () => {
    expect(formatCurrency(null)).toBe('—');
    expect(formatCurrency(undefined)).toBe('—');
    expect(formatCurrency(Number.NaN)).toBe('—');
  });

  it('formats USD values with 0 fraction digits by default', () => {
    expect(formatCurrency(0)).toBe('$0');
    expect(formatCurrency(38950)).toBe('$38,950');
    expect(formatCurrency(38950.6)).toBe('$38,951');
    expect(formatCurrency(1250000)).toBe('$1,250,000');
  });

  it('formats with a custom currency code', () => {
    expect(formatCurrency(50000, 'EUR')).toBe('€50,000');
    expect(formatCurrency(50000, 'GBP')).toBe('£50,000');
  });

  it('falls back gracefully for empty or invalid currency codes', () => {
    expect(formatCurrency(1500, '')).toBe('$1,500');
    expect(formatCurrency(1500, 'INVALID')).toBe('$1,500');
  });
});
