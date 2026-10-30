import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Vehicle } from '../../../../core/models/vehicle.model';
import { http } from '../../../../core/services/api.service';
import { store } from '../../../../core/store/store';
import { toastsCleared } from '../../../../core/store/toast.slice';
import { LogActionModalComponent } from './log-action-modal';

const sampleVehicle: Vehicle = {
  id: 'veh-123',
  dealershipId: 'dealer-1',
  vin: '1HGBH41JXMN109186',
  stockNumber: 'STK-101',
  make: 'Toyota',
  model: 'Camry',
  year: 2024,
  color: 'Silver',
  mileage: 12000,
  fuelType: 'Hybrid',
  purchasePriceAmount: 24000,
  purchasePriceCurrency: 'USD',
  askingPriceAmount: 27500,
  askingPriceCurrency: 'USD',
  status: 'Available',
  dateAddedToInventoryUtc: '2026-09-01T00:00:00Z',
  daysInInventory: 58,
  isAging: false,
  agingSeverity: 'Warning',
  demandScore: 72,
  demandLevel: 'Medium',
  createdAtUtc: '2026-09-01T00:00:00Z',
  updatedAtUtc: '2026-09-01T00:00:00Z',
  rowVersion: 'AAAAAAAAB9E='
};

describe('LogActionModalComponent', () => {
  const originalAdapter = http.defaults.adapter;

  beforeEach(() => {
    store.dispatch(toastsCleared());
  });

  afterEach(() => {
    http.defaults.adapter = originalAdapter;
    store.dispatch(toastsCleared());
  });

  it('records an action and invokes onActionLogged with data.id without error toasts', async () => {
    http.defaults.adapter = async config => {
      if (config.method === 'post') {
        return {
          data: { data: { id: 'action-999' } },
          status: 201,
          statusText: 'Created',
          headers: {},
          config
        };
      }
      return {
        data: { data: [], meta: { page: 1, limit: 50, total: 0 } },
        status: 200,
        statusText: 'OK',
        headers: {},
        config
      };
    };

    const onActionLogged = vi.fn();
    render(
      <Provider store={store}>
        <LogActionModalComponent
          vehicle={sampleVehicle}
          onClose={() => {}}
          onActionLogged={onActionLogged}
        />
      </Provider>
    );

    await screen.findByText(/No previous actions logged for this vehicle yet/i);
    fireEvent.click(screen.getByRole('button', { name: /Record Action/i }));

    await waitFor(() => {
      expect(onActionLogged).toHaveBeenCalledWith('action-999');
      expect(screen.getByRole('button', { name: /Record Action/i })).not.toBeDisabled();
    });

    const toasts = store.getState().toast.toasts;
    expect(toasts.some(t => t.title === 'Action Recorded')).toBe(true);
    expect(toasts.some(t => t.title === 'Action Failed')).toBe(false);
  });

  it('gracefully handles an empty 201 body without firing Action Failed toast', async () => {
    http.defaults.adapter = async config => {
      if (config.method === 'post') {
        return {
          data: '',
          status: 201,
          statusText: 'Created',
          headers: {},
          config
        };
      }
      return {
        data: { data: [], meta: { page: 1, limit: 50, total: 0 } },
        status: 200,
        statusText: 'OK',
        headers: {},
        config
      };
    };

    const onActionLogged = vi.fn();
    render(
      <Provider store={store}>
        <LogActionModalComponent
          vehicle={sampleVehicle}
          onClose={() => {}}
          onActionLogged={onActionLogged}
        />
      </Provider>
    );

    await screen.findByText(/No previous actions logged for this vehicle yet/i);
    fireEvent.click(screen.getByRole('button', { name: /Record Action/i }));

    await waitFor(() => {
      expect(onActionLogged).toHaveBeenCalledWith('');
      expect(screen.getByRole('button', { name: /Record Action/i })).not.toBeDisabled();
    });

    const toasts = store.getState().toast.toasts;
    expect(toasts.some(t => t.title === 'Action Recorded')).toBe(true);
    expect(toasts.some(t => t.title === 'Action Failed')).toBe(false);
  });
});
