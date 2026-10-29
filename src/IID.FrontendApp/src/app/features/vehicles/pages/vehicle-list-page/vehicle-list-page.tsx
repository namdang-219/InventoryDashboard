import { useCallback, useEffect, useRef, useState } from 'react';
import { useLocation, useSearchParams } from 'react-router-dom';
import { Vehicle, VehicleFilterParams } from '../../../../core/models/vehicle.model';
import { useAuth } from '../../../../core/services/auth.service';
import { useDealerships } from '../../../../core/services/dealership.service';
import { inventoryService } from '../../../../core/services/inventory.service';
import { useRealtimeEvent } from '../../../../core/services/realtime.service';
import { useToast } from '../../../../core/services/toast.service';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { CreateVehicleModalComponent } from '../../components/create-vehicle-modal/create-vehicle-modal';
import { EditVehicleModalComponent } from '../../components/edit-vehicle-modal/edit-vehicle-modal';
import { LogActionModalComponent } from '../../components/log-action-modal/log-action-modal';
import { MarkSoldModalComponent } from '../../components/mark-sold-modal/mark-sold-modal';
import { TransferDealershipModalComponent } from '../../components/transfer-dealership-modal/transfer-dealership-modal';
import { VehicleCardComponent } from '../../components/vehicle-card/vehicle-card';
import { VehicleDetailModalComponent } from '../../components/vehicle-detail-modal/vehicle-detail-modal';
import { VehicleFilterBarComponent } from '../../components/vehicle-filter-bar/vehicle-filter-bar';
import { VehicleTableComponent } from '../../components/vehicle-table/vehicle-table';
import './vehicle-list-page.scss';

/**
 * Suppresses redundant realtime-triggered reloads immediately after a
 * local action (create/edit/sold/action). Without this cooldown, an edit
 * triggers 4 GETs: explicit local refresh, realtime `VehicleUpdated`,
 * realtime `InventoryChanged`, plus an optional detail fetch.
 */
const REALTIME_SUPPRESS_MS = 1500;
const PAGE_LIMIT = 20;

export function VehicleListPageComponent() {
  const auth = useAuth();
  const { selectedDealershipId } = useDealerships();
  const toast = useToast();
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();

  const isManager = auth.isManager;
  const canMarkSold = auth.canMarkSold;
  const isAgingOnly = location.pathname.includes('aging');

  // View state
  const [viewMode, setViewMode] = useState<'grid' | 'table'>('table');
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Data
  const [vehicles, setVehicles] = useState<readonly Vehicle[]>([]);
  const [page, setPage] = useState<number>(1);
  const [total, setTotal] = useState<number>(0);

  // Modals
  const [showCreateModal, setShowCreateModal] = useState<boolean>(false);
  const [vehicleForEdit, setVehicleForEdit] = useState<Vehicle | null>(null);
  const [vehicleForSold, setVehicleForSold] = useState<Vehicle | null>(null);
  const [vehicleForAction, setVehicleForAction] = useState<Vehicle | null>(null);
  const [vehicleForTransfer, setVehicleForTransfer] = useState<Vehicle | null>(null);
  const [vehicleForDetail, setVehicleForDetail] = useState<Vehicle | null>(null);
  const [detailModalLoading, setDetailModalLoading] = useState<boolean>(false);

  // Mutable refs for latest state in async/realtime callbacks
  const lastLocalActionAtUtcRef = useRef<number>(0);
  const currentFiltersRef = useRef<VehicleFilterParams>({});
  const pageRef = useRef<number>(1);
  const vehiclesRef = useRef<readonly Vehicle[]>(vehicles);
  vehiclesRef.current = vehicles;
  const isAgingOnlyRef = useRef<boolean>(isAgingOnly);
  isAgingOnlyRef.current = isAgingOnly;
  const selectedDealershipIdRef = useRef<string | null>(selectedDealershipId);
  selectedDealershipIdRef.current = selectedDealershipId;
  const prevIsAgingRef = useRef<boolean>(isAgingOnly);
  const requestIdRef = useRef<number>(0);

  function shouldIgnoreRealtimeReload(): boolean {
    return Date.now() - lastLocalActionAtUtcRef.current < REALTIME_SUPPRESS_MS;
  }

  const loadVehicles = useCallback((showLoading = true, pageOverride?: number) => {
    const reqId = ++requestIdRef.current;
    if (showLoading) setLoading(true);
    setError(null);

    const activePage = pageOverride ?? pageRef.current;
    const dealerId = selectedDealershipIdRef.current ?? undefined;

    if (isAgingOnlyRef.current) {
      inventoryService
        .getAgingStock(activePage, PAGE_LIMIT, dealerId)
        .then(res => {
          if (reqId !== requestIdRef.current) return;
          setVehicles(res.data);
          setTotal(res.meta.total);
          setLoading(false);
        })
        .catch((err: unknown) => {
          if (reqId !== requestIdRef.current) return;
          setLoading(false);
          const anyErr = err as { error?: { message?: string } } | null;
          setError(anyErr?.error?.message || 'Failed to load aging stock.');
        });
    } else {
      const params: VehicleFilterParams = {
        ...currentFiltersRef.current,
        dealershipId: dealerId,
        page: activePage,
        limit: PAGE_LIMIT
      };

      inventoryService
        .listVehicles(params)
        .then(res => {
          if (reqId !== requestIdRef.current) return;
          setVehicles(res.data);
          setTotal(res.meta.total);
          setLoading(false);
        })
        .catch((err: unknown) => {
          if (reqId !== requestIdRef.current) return;
          setLoading(false);
          const anyErr = err as { error?: { message?: string } } | null;
          setError(anyErr?.error?.message || 'Failed to load vehicles from API.');
        });
    }
  }, []);

  // Reload on route mode (full vs aging) or active dealership change
  useEffect(() => {
    if (prevIsAgingRef.current !== isAgingOnly) {
      prevIsAgingRef.current = isAgingOnly;
      currentFiltersRef.current = {};
    }
    pageRef.current = 1;
    setPage(1);
    loadVehicles(true, 1);
  }, [isAgingOnly, selectedDealershipId, loadVehicles]);

  // Realtime events handling
  useRealtimeEvent('vehicleAdded', () => {
    if (shouldIgnoreRealtimeReload()) return;
    loadVehicles(false);
  });

  useRealtimeEvent('vehicleUpdated', () => {
    if (shouldIgnoreRealtimeReload()) return;
    loadVehicles(false);
  });

  useRealtimeEvent('vehicleRemoved', id => {
    setVehicles(list => list.filter(v => v.id !== id));
    setTotal(t => Math.max(0, t - 1));
  });

  useRealtimeEvent('inventoryChanged', () => {
    if (shouldIgnoreRealtimeReload()) return;
    loadVehicles(false);
  });

  const clearDeepLinkParam = useCallback(() => {
    setSearchParams(
      prev => {
        const next = new URLSearchParams(prev);
        next.delete('vin');
        return next;
      },
      { replace: true }
    );
  }, [setSearchParams]);

  // Deep-link from dashboard "Take Action": /vehicles?vin={vehicleId}
  const vinParam = searchParams.get('vin');
  useEffect(() => {
    if (!vinParam) return;

    const existing = vehiclesRef.current.find(v => v.id === vinParam);
    if (existing) {
      setVehicleForDetail(existing);
      clearDeepLinkParam();
      return;
    }

    let cancelled = false;
    setDetailModalLoading(true);
    inventoryService
      .getVehicleById(vinParam)
      .then(res => {
        if (cancelled) return;
        setVehicleForDetail(res.data);
        setDetailModalLoading(false);
        clearDeepLinkParam();
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setDetailModalLoading(false);
        clearDeepLinkParam();
        const anyErr = err as { error?: { message?: string } } | null;
        toast.error(
          'Vehicle Not Found',
          anyErr?.error?.message || `Could not locate vehicle ${vinParam}.`
        );
      });

    return () => {
      cancelled = true;
    };
  }, [vinParam, clearDeepLinkParam, toast]);

  const onFilterChange = useCallback(
    (params: VehicleFilterParams) => {
      currentFiltersRef.current = params;
      pageRef.current = 1;
      setPage(1);
      loadVehicles(true, 1);
    },
    [loadVehicles]
  );

  function changePage(delta: number): void {
    const next = pageRef.current + delta;
    if (next < 1) return;
    const maxPage = Math.ceil(total / PAGE_LIMIT);
    if (maxPage > 0 && next > maxPage) return;
    pageRef.current = next;
    setPage(next);
    loadVehicles(true, next);
  }

  function openCreateModal(): void {
    if (!isManager) {
      toast.warning('Permission Denied', 'Only Managers can register new vehicles.');
      return;
    }
    setShowCreateModal(true);
  }

  function onVehicleCreated(): void {
    lastLocalActionAtUtcRef.current = Date.now();
    loadVehicles(true);
  }

  function openMarkSold(v: Vehicle): void {
    if (!canMarkSold) {
      toast.warning('Permission Denied', 'Only Sales can mark vehicles as sold.');
      return;
    }
    setVehicleForSold(v);
  }

  function openLogAction(v: Vehicle): void {
    if (!isManager) {
      toast.warning('Permission Denied', 'Only Managers can log vehicle actions.');
      return;
    }
    setVehicleForAction(v);
  }

  function openTransferDealership(v: Vehicle): void {
    if (!isManager) {
      toast.warning('Permission Denied', 'Only Managers can transfer vehicle showrooms.');
      return;
    }
    setVehicleForTransfer(v);
  }

  function openDetail(v: Vehicle): void {
    setVehicleForDetail(v);
  }

  function openEditVehicle(v: Vehicle): void {
    if (!isManager) {
      toast.warning('Permission Denied', 'Only Managers can edit vehicles.');
      return;
    }
    setVehicleForEdit(v);
  }

  function onVehicleUpdated(): void {
    lastLocalActionAtUtcRef.current = Date.now();
    loadVehicles(false);
    if (vehicleForDetail) {
      inventoryService
        .getVehicleById(vehicleForDetail.id)
        .then(res => setVehicleForDetail(res.data))
        .catch(() => setVehicleForDetail(null));
    }
  }

  function onSoldSuccess(): void {
    lastLocalActionAtUtcRef.current = Date.now();
    loadVehicles(false);
  }

  function onTransferSuccess(result: { vehicleId: string; targetDealershipId: string }): void {
    lastLocalActionAtUtcRef.current = Date.now();
    loadVehicles(false);
    if (vehicleForDetail && vehicleForDetail.id === result.vehicleId) {
      inventoryService
        .getVehicleById(result.vehicleId)
        .then(res => setVehicleForDetail(res.data))
        .catch(() => setVehicleForDetail(null));
    }
  }

  function onActionSuccess(): void {
    lastLocalActionAtUtcRef.current = Date.now();
    loadVehicles(false);
  }

  return (
    <>
      <div className="iid-vehicle-list-page vehicles-page">
        {/* Top Bar */}
        <div className="page-topbar">
          <div className="topbar-title">
            <h2>{isAgingOnly ? 'Aging Stock Management' : 'Vehicle Inventory Fleet'}</h2>
            <p>
              {isAgingOnly
                ? 'Units exceeding aging velocity thresholds requiring intervention, repricing, or wholesale routing.'
                : 'Complete live vehicle registry with demand intelligence, telemetry and optimistic concurrency control.'}
            </p>
          </div>

          <div className="topbar-actions">
            {/* Grid / Table View Switcher */}
            <div className="view-toggle">
              <button
                type="button"
                className={`toggle-btn${viewMode === 'table' ? ' active' : ''}`}
                onClick={() => setViewMode('table')}
                title="Table View"
              >
                <span className="material-symbols-outlined">table_rows</span>
              </button>
              <button
                type="button"
                className={`toggle-btn${viewMode === 'grid' ? ' active' : ''}`}
                onClick={() => setViewMode('grid')}
                title="Grid View"
              >
                <span className="material-symbols-outlined">grid_view</span>
              </button>
            </div>

            {/* Add Vehicle Button */}
            {isManager ? (
              <button type="button" className="iid-btn iid-btn-primary" onClick={openCreateModal}>
                <span className="material-symbols-outlined">add_circle</span>
                <span>Register Vehicle</span>
              </button>
            ) : (
              <div className="readonly-badge" title="Logged in with Sales role">
                <span className="material-symbols-outlined">badge</span>
                <span>Sales Mode</span>
              </div>
            )}
          </div>
        </div>

        {/* Filter Bar (when on full list) */}
        {!isAgingOnly && <VehicleFilterBarComponent onFilterChange={onFilterChange} />}

        {/* Data Display */}
        {loading ? (
          <LoadingSpinnerComponent message="Loading vehicle fleet from database..." size="lg" />
        ) : error ? (
          <div className="error-banner iid-card">
            <div className="error-left">
              <span className="material-symbols-outlined error-icon">error</span>
              <div>
                <h4>Failed to Load Vehicles</h4>
                <p>{error}</p>
              </div>
            </div>
            <button
              type="button"
              className="iid-btn iid-btn-primary iid-btn-sm"
              onClick={() => loadVehicles(true)}
            >
              Retry
            </button>
          </div>
        ) : vehicles.length === 0 ? (
          <EmptyStateComponent
            icon="directions_car"
            title={isAgingOnly ? 'No Aging Stock' : 'No Vehicles Found'}
            description={
              isAgingOnly
                ? 'Congratulations! No vehicles currently exceed lot threshold limits.'
                : 'No vehicles match your current filter parameters.'
            }
            actionLabel={isManager && !isAgingOnly ? 'Register First Vehicle' : null}
            onActionClick={openCreateModal}
          />
        ) : (
          <>
            {/* Grid or Table View */}
            {viewMode === 'table' ? (
              <VehicleTableComponent
                vehicles={vehicles}
                isManager={isManager}
                canMarkSold={canMarkSold}
                onViewDetails={openDetail}
                onEditVehicle={openEditVehicle}
                onMarkSold={openMarkSold}
                onLogAction={openLogAction}
                onTransferDealership={openTransferDealership}
              />
            ) : (
              <div className="cards-grid">
                {vehicles.map(veh => (
                  <VehicleCardComponent
                    key={veh.id}
                    vehicle={veh}
                    isManager={isManager}
                    canMarkSold={canMarkSold}
                    onViewDetails={openDetail}
                    onEditVehicle={openEditVehicle}
                    onMarkSold={openMarkSold}
                    onLogAction={openLogAction}
                    onTransferDealership={openTransferDealership}
                  />
                ))}
              </div>
            )}

            {/* Pagination Controls */}
            <div className="pagination-bar iid-card">
              <div className="page-info">
                Showing <strong>{vehicles.length}</strong> of <strong>{total}</strong> vehicles
                (Page {page})
              </div>

              <div className="page-nav">
                <button
                  type="button"
                  className="iid-btn iid-btn-secondary iid-btn-sm"
                  disabled={page <= 1}
                  onClick={() => changePage(-1)}
                >
                  <span className="material-symbols-outlined">chevron_left</span>
                  <span>Previous</span>
                </button>
                <button
                  type="button"
                  className="iid-btn iid-btn-secondary iid-btn-sm"
                  disabled={page * PAGE_LIMIT >= total || vehicles.length === 0}
                  onClick={() => changePage(1)}
                >
                  <span>Next</span>
                  <span className="material-symbols-outlined">chevron_right</span>
                </button>
              </div>
            </div>
          </>
        )}
      </div>

      {/* Modals */}
      {showCreateModal && (
        <CreateVehicleModalComponent
          onClose={() => setShowCreateModal(false)}
          onCreated={onVehicleCreated}
        />
      )}

      {vehicleForSold && (
        <MarkSoldModalComponent
          vehicle={vehicleForSold}
          isManager={isManager}
          onClose={() => setVehicleForSold(null)}
          onSoldSuccess={onSoldSuccess}
        />
      )}

      {vehicleForAction && (
        <LogActionModalComponent
          vehicle={vehicleForAction}
          onClose={() => setVehicleForAction(null)}
          onActionLogged={onActionSuccess}
        />
      )}

      {vehicleForEdit && (
        <EditVehicleModalComponent
          vehicle={vehicleForEdit}
          onClose={() => setVehicleForEdit(null)}
          onUpdated={onVehicleUpdated}
        />
      )}

      {vehicleForDetail && (
        <VehicleDetailModalComponent
          vehicle={vehicleForDetail}
          isManager={isManager}
          canMarkSold={canMarkSold}
          onClose={() => setVehicleForDetail(null)}
          onEditVehicle={v => {
            setVehicleForDetail(null);
            openEditVehicle(v);
          }}
          onMarkSold={v => {
            setVehicleForDetail(null);
            openMarkSold(v);
          }}
          onLogAction={v => {
            setVehicleForDetail(null);
            openLogAction(v);
          }}
          onTransferDealership={v => {
            setVehicleForDetail(null);
            openTransferDealership(v);
          }}
        />
      )}

      {vehicleForTransfer && (
        <TransferDealershipModalComponent
          vehicle={vehicleForTransfer}
          onClose={() => setVehicleForTransfer(null)}
          onTransferred={onTransferSuccess}
        />
      )}

      {detailModalLoading && (
        <div className="iid-modal-backdrop">
          <LoadingSpinnerComponent message="Locating vehicle from inventory..." size="lg" />
        </div>
      )}
    </>
  );
}
