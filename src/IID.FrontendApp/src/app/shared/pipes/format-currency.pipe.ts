export function formatCurrency(value: number | null | undefined, currency = 'USD'): string {
  if (value === null || value === undefined || isNaN(value)) {
    return '—';
  }

  try {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency || 'USD',
      maximumFractionDigits: 0
    }).format(value);
  } catch {
    return `$${value.toLocaleString('en-US')}`;
  }
}
