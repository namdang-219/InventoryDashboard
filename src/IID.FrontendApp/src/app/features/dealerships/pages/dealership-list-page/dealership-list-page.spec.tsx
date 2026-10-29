import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { AxiosAdapter } from 'axios';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { Dealership } from '../../../../core/models/dealership.model';
import { http } from '../../../../core/services/api.service';
import { realtimeEvents } from '../../../../core/services/realtime.service';
import { authCleared, authSucceeded } from '../../../../core/store/auth.slice';
import {
  dealershipsError,
  dealershipsLoading,
  dealershipsSet,
  recentlyUpdatedDealershipSet,
  selectedDealershipSet
} from '../../../../core/store/dealership.slice';
import { connectionStatusSet } from '../../../../core/store/realtime.slice';
import { store } from '../../../../core/store/store';
import { DealershipListPageComponent } from './dealership-list-page';

describe('DealershipListPageComponent Realtime UI', () => {
  const originalAdapter = http.defaults.adapter;

  const mockDealerships: Dealership[] = [
    {
      id: 'dealer-1',
      name: 'Austin Showroom',
      code: 'ATX-01',
      city: 'Austin',
      state: 'TX',
      phone: '512-555-0100',
      vehicleCount: 10
    },
    {
      id: 'dealer-2',
      name: 'Dallas Showroom',
      code: 'DAL-01',
      city: 'Dallas',
      state: 'TX',
      phone: '214-555-0200',
      vehicleCount: 20
    }
  ];

  beforeEach(() => {
    localStorage.clear();

    store.dispatch(authCleared());
    store.dispatch(dealershipsSet([]));
    store.dispatch(selectedDealershipSet(null));
    store.dispatch(recentlyUpdatedDealershipSet(null));
    store.dispatch(dealershipsError(null));
    store.dispatch(dealershipsLoading(false));
    store.dispatch(connectionStatusSet('disconnected'));

    const mockAdapter: AxiosAdapter = async config => ({
      data: { data: mockDealerships },
      status: 200,
      statusText: 'OK',
      headers: {},
      config
    });
    http.defaults.adapter = mockAdapter;
  });

  afterEach(() => {
    http.defaults.adapter = originalAdapter;
    localStorage.clear();
  });

  function renderPage() {
    return render(
      <Provider store={store}>
        <MemoryRouter initialEntries={['/dealerships']}>
          <DealershipListPageComponent />
        </MemoryRouter>
      </Provider>
    );
  }

  it('should create and render dealership cards', async () => {
    const { container } = renderPage();

    await waitFor(() => {
      const cards = container.querySelectorAll('.dealer-card');
      expect(cards.length).toBe(2);
    });

    expect(screen.getAllByText('Austin Showroom').length).toBeGreaterThan(0);
    expect(screen.getByText('Dallas Showroom')).toBeInTheDocument();
  });

  it('should display connected realtime badge in topbar', async () => {
    const { container } = renderPage();

    await waitFor(() => {
      expect(container.querySelectorAll('.dealer-card').length).toBe(2);
    });

    act(() => {
      store.dispatch(connectionStatusSet('connected'));
    });

    const pill = container.querySelector('.realtime-status-pill');
    expect(pill).toBeTruthy();
    expect(pill).toHaveClass('connected');
    expect(pill?.textContent).toContain('Live Network');
  });

  it('should display realtime badge and glow styling on card when dealership is updated via SignalR', async () => {
    const { container } = renderPage();

    await waitFor(() => {
      expect(container.querySelectorAll('.dealer-card').length).toBe(2);
    });

    http.defaults.adapter = async config => ({
      data: {
        data: [
          mockDealerships[0],
          { ...mockDealerships[1], name: 'Dallas Flagship Center', code: 'DAL-99', phone: '214-555-9999' }
        ]
      },
      status: 200,
      statusText: 'OK',
      headers: {},
      config
    });

    await act(async () => {
      realtimeEvents.emit('dealershipUpdated', {
        id: 'dealer-2',
        name: 'Dallas Flagship Center',
        code: 'DAL-99',
        city: 'Dallas',
        state: 'TX',
        phone: '214-555-9999'
      });
    });

    const cards = container.querySelectorAll('.dealer-card');
    const updatedCard = cards[1] as HTMLElement;

    expect(updatedCard).toHaveClass('is-recently-updated');
    expect(updatedCard.querySelector('.realtime-badge')).toBeTruthy();
    expect(updatedCard.querySelector('.realtime-badge')?.textContent).toContain('Live Update');
    expect(updatedCard.querySelector('.dealer-name')?.textContent).toContain('Dallas Flagship Center');
  });

  it('should keep currently opened edit modal synced if realtime update arrives for that dealership', async () => {
    store.dispatch(
      authSucceeded({
        token: 'manager-token',
        user: { id: 'u-1', email: 'admin@iid.local', roles: ['Manager'] }
      })
    );

    const { container } = renderPage();

    await waitFor(() => {
      expect(container.querySelectorAll('.dealer-card').length).toBe(2);
    });

    const firstCardEditBtn = container.querySelectorAll('.edit-btn')[0] as HTMLButtonElement;
    expect(firstCardEditBtn).toBeTruthy();

    fireEvent.click(firstCardEditBtn);

    const nameInput = screen.getByLabelText(/Showroom \/ Dealership Name/i) as HTMLInputElement;
    const codeInput = screen.getByLabelText(/Showroom Code/i) as HTMLInputElement;
    expect(nameInput.value).toBe('Austin Showroom');
    expect(codeInput.value).toBe('ATX-01');

    http.defaults.adapter = async config => ({
      data: {
        data: [
          { ...mockDealerships[0], name: 'Austin Innovation Center', code: 'ATX-02', phone: '512-555-0222' },
          mockDealerships[1]
        ]
      },
      status: 200,
      statusText: 'OK',
      headers: {},
      config
    });

    await act(async () => {
      realtimeEvents.emit('dealershipUpdated', {
        id: 'dealer-1',
        name: 'Austin Innovation Center',
        code: 'ATX-02',
        city: 'Austin',
        state: 'TX',
        phone: '512-555-0222'
      });
    });

    // Modal target inputs should be reactively updated
    expect(nameInput.value).toBe('Austin Innovation Center');
    expect(codeInput.value).toBe('ATX-02');
  });
});
