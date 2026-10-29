import clsx from 'clsx';
import { KeyboardEvent, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Dealership } from '../../../../core/models/dealership.model';
import { useAuth } from '../../../../core/services/auth.service';
import { dealershipService, useDealerships } from '../../../../core/services/dealership.service';
import { useRealtime, useRealtimeEvent } from '../../../../core/services/realtime.service';
import { store } from '../../../../core/store/store';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { DealershipFormModalComponent } from '../../components/dealership-form-modal/dealership-form-modal';
import './dealership-list-page.scss';

const SEARCH_DEBOUNCE_MS = 250;

export function DealershipListPageComponent() {
  const {
    dealerships,
    selectedDealership,
    selectedDealershipId,
    isLoading,
    error,
    recentlyUpdatedDealershipId,
    loadDealerships,
    selectDealership
  } = useDealerships();
  const { connectionStatus } = useRealtime();
  const { isManager } = useAuth();
  const navigate = useNavigate();

  const [searchInputText, setSearchInputText] = useState('');
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [selectedForEdit, setSelectedForEdit] = useState<Dealership | null>(null);

  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const current = store.getState().dealership;
    if (!current.isLoading && (current.dealerships.length === 0 || current.error)) {
      dealershipService.loadDealerships();
    }
  }, []);

  useEffect(() => {
    return () => {
      if (debounceTimerRef.current) {
        clearTimeout(debounceTimerRef.current);
      }
    };
  }, []);

  // If an update arrives for the dealership currently opened in the edit modal, keep it synced
  useRealtimeEvent('dealershipUpdated', updated => {
    setSelectedForEdit(curEdit =>
      curEdit && curEdit.id === updated.id
        ? {
            ...curEdit,
            name: updated.name,
            code: updated.code,
            city: updated.city,
            state: updated.state,
            phone: updated.phone
          }
        : curEdit
    );
  });

  const onSearchInput = (value: string): void => {
    setSearchInputText(value);
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }
    debounceTimerRef.current = setTimeout(() => {
      setDebouncedQuery(value);
      debounceTimerRef.current = null;
    }, SEARCH_DEBOUNCE_MS);
  };

  const flushSearch = (): void => {
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
      debounceTimerRef.current = null;
    }
    setDebouncedQuery(searchInputText);
  };

  const clearSearch = (): void => {
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
      debounceTimerRef.current = null;
    }
    setSearchInputText('');
    setDebouncedQuery('');
  };

  const onSearchKeyDown = (e: KeyboardEvent<HTMLInputElement>): void => {
    if (e.key === 'Enter') {
      flushSearch();
    }
  };

  const filteredDealerships = useMemo(() => {
    const query = debouncedQuery.trim().toLowerCase();
    if (!query) return dealerships;

    return dealerships.filter(
      d =>
        d.name.toLowerCase().includes(query) ||
        d.code.toLowerCase().includes(query) ||
        d.city.toLowerCase().includes(query) ||
        d.state.toLowerCase().includes(query) ||
        Boolean(d.phone && d.phone.toLowerCase().includes(query))
    );
  }, [dealerships, debouncedQuery]);

  const totalShowrooms = dealerships.length;

  const totalVehicles = useMemo(
    () => dealerships.reduce((sum, d) => sum + (d.vehicleCount ?? 0), 0),
    [dealerships]
  );

  const totalStates = useMemo(
    () => new Set(dealerships.map(d => d.state)).size,
    [dealerships]
  );

  const openCreateModal = (): void => {
    setSelectedForEdit(null);
    setShowModal(true);
  };

  const openEditModal = (dealership: Dealership): void => {
    setSelectedForEdit(dealership);
    setShowModal(true);
  };

  const viewDealershipInventory = (d: Dealership): void => {
    selectDealership(d);
    navigate('/vehicles');
  };

  const onModalSaved = (): void => {
    loadDealerships(false);
  };

  return (
    <>
      <div className="iid-dealership-list-page dealerships-page">
        {/* Top Bar */}
        <div className="page-topbar">
          <div className="topbar-title">
            <h2>Dealership Network</h2>
            <p>Manage retail showrooms, lot allocations, and regional automotive distribution hubs.</p>
          </div>

          <div className="topbar-actions">
            <div
              className={clsx(
                'realtime-status-pill',
                connectionStatus === 'connected' && 'connected',
                (connectionStatus === 'connecting' || connectionStatus === 'reconnecting') &&
                  'connecting'
              )}
              title={`SignalR Real-Time Network: ${connectionStatus}`}
            >
              <span className="status-pulse-dot"></span>
              <span className="status-label">
                {connectionStatus === 'connected'
                  ? 'Live Network'
                  : connectionStatus === 'connecting' || connectionStatus === 'reconnecting'
                    ? 'Connecting...'
                    : 'Offline'}
              </span>
            </div>

            <button
              type="button"
              className="iid-btn iid-btn-secondary"
              onClick={() => loadDealerships()}
              disabled={isLoading}
            >
              <span className={clsx('material-symbols-outlined', isLoading && 'spin-icon')}>
                refresh
              </span>
              <span>Refresh</span>
            </button>

            {isManager && (
              <button type="button" className="iid-btn iid-btn-primary" onClick={openCreateModal}>
                <span className="material-symbols-outlined">add_business</span>
                <span>Register Dealership</span>
              </button>
            )}
          </div>
        </div>

        {/* Metric Telemetry Cards */}
        <div className="metrics-grid">
          <div className="metric-card iid-card">
            <div className="metric-icon-wrap icon-purple">
              <span className="material-symbols-outlined">storefront</span>
            </div>
            <div className="metric-content">
              <span className="metric-label">Total Showrooms</span>
              <div className="metric-value">{totalShowrooms}</div>
              <span className="metric-sub">Active network points</span>
            </div>
          </div>

          <div className="metric-card iid-card">
            <div className="metric-icon-wrap icon-blue">
              <span className="material-symbols-outlined">directions_car</span>
            </div>
            <div className="metric-content">
              <span className="metric-label">Network Inventory</span>
              <div className="metric-value">{totalVehicles}</div>
              <span className="metric-sub">Vehicles distributed across lots</span>
            </div>
          </div>

          <div className="metric-card iid-card">
            <div className="metric-icon-wrap icon-emerald">
              <span className="material-symbols-outlined">public</span>
            </div>
            <div className="metric-content">
              <span className="metric-label">Regional Footprint</span>
              <div className="metric-value">{totalStates}</div>
              <span className="metric-sub">States & regional hubs</span>
            </div>
          </div>

          <div className="metric-card iid-card active-hub-card">
            <div className="metric-icon-wrap icon-amber">
              <span className="material-symbols-outlined">verified</span>
            </div>
            <div className="metric-content">
              <span className="metric-label">Active Global Showroom</span>
              <div className="metric-value active-name">
                {selectedDealership?.name || 'All Dealerships'}
              </div>
              <span className="metric-sub">
                {selectedDealership?.city ?? ''}, {selectedDealership?.state ?? ''}
              </span>
            </div>
          </div>
        </div>

        {/* Search Filter Bar */}
        <div className="filter-bar iid-card">
          <div className="search-box">
            <span className="material-symbols-outlined search-icon">search</span>
            <input
              type="text"
              className="search-input"
              placeholder="Search dealerships by name, code (e.g. DAL-01), city or state..."
              value={searchInputText}
              onChange={e => onSearchInput(e.target.value)}
              onKeyDown={onSearchKeyDown}
            />
            {searchInputText && (
              <button type="button" className="clear-btn" onClick={clearSearch}>
                <span className="material-symbols-outlined">close</span>
              </button>
            )}
          </div>
          <div className="results-count">
            Showing <strong>{filteredDealerships.length}</strong> of{' '}
            <strong>{totalShowrooms}</strong> showrooms
          </div>
        </div>

        {/* Content State */}
        {isLoading ? (
          <LoadingSpinnerComponent message="Loading dealership network fleet..." size="lg" />
        ) : error ? (
          <div className="error-banner iid-card">
            <div className="error-left">
              <span className="material-symbols-outlined error-icon">error</span>
              <div>
                <h4>Failed to load dealerships</h4>
                <p>{error}</p>
              </div>
            </div>
            <button
              type="button"
              className="iid-btn iid-btn-primary iid-btn-sm"
              onClick={() => loadDealerships()}
            >
              Retry
            </button>
          </div>
        ) : filteredDealerships.length === 0 ? (
          <EmptyStateComponent
            icon="storefront"
            title="No Dealerships Found"
            description="No showrooms match your search query."
            actionLabel={isManager ? 'Register Dealership' : null}
            onActionClick={openCreateModal}
          />
        ) : (
          /* Dealership Cards Grid */
          <div className="dealerships-grid">
            {filteredDealerships.map(d => (
              <div
                key={d.id}
                className={clsx(
                  'dealer-card iid-card',
                  d.id === selectedDealershipId && 'is-active-dealer',
                  d.id === recentlyUpdatedDealershipId && 'is-recently-updated'
                )}
              >
                <div className="dealer-topbar">
                  <div className="dealer-code-badge">{d.code}</div>
                  <div className="dealer-top-actions">
                    {d.id === recentlyUpdatedDealershipId && (
                      <div className="realtime-badge">
                        <span className="material-symbols-outlined">bolt</span>
                        <span>Live Update</span>
                      </div>
                    )}
                    {d.id === selectedDealershipId && (
                      <div className="active-badge">
                        <span className="material-symbols-outlined">check_circle</span>
                        <span>Selected</span>
                      </div>
                    )}
                    {isManager && (
                      <button
                        type="button"
                        className="iid-btn-icon edit-btn"
                        title="Edit Dealership Info"
                        onClick={() => openEditModal(d)}
                      >
                        <span className="material-symbols-outlined">edit</span>
                      </button>
                    )}
                  </div>
                </div>

                <h3 className="dealer-name">{d.name}</h3>

                <div className="dealer-details">
                  <div className="detail-row">
                    <span className="material-symbols-outlined detail-icon">location_on</span>
                    <span className="detail-text">
                      {d.city}, {d.state}
                    </span>
                  </div>
                  {d.phone && (
                    <div className="detail-row">
                      <span className="material-symbols-outlined detail-icon">phone</span>
                      <span className="detail-text font-mono">{d.phone}</span>
                    </div>
                  )}
                </div>

                {/* Inventory Count Box */}
                <div className="inventory-status-box">
                  <div className="inv-left">
                    <span className="material-symbols-outlined inv-icon">directions_car</span>
                    <span className="inv-label">On-Lot Fleet</span>
                  </div>
                  <span className="inv-count">{d.vehicleCount ?? 0} units</span>
                </div>

                {/* Card Actions */}
                <div className="dealer-card-actions">
                  {d.id !== selectedDealershipId && (
                    <button
                      type="button"
                      className="iid-btn iid-btn-secondary iid-btn-sm set-active-btn"
                      onClick={() => selectDealership(d)}
                      title="Select this dealership for global dashboard metrics"
                    >
                      <span className="material-symbols-outlined">storefront</span>
                      <span>Select</span>
                    </button>
                  )}

                  <button
                    type="button"
                    className="iid-btn iid-btn-primary iid-btn-sm view-fleet-btn"
                    onClick={() => viewDealershipInventory(d)}
                    title="View vehicle stock at this dealership"
                  >
                    <span className="material-symbols-outlined">inventory_2</span>
                    <span>View Fleet</span>
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Add / Edit Modal */}
      {showModal && (
        <DealershipFormModalComponent
          dealership={selectedForEdit}
          onClose={() => setShowModal(false)}
          onSaved={onModalSaved}
        />
      )}
    </>
  );
}
