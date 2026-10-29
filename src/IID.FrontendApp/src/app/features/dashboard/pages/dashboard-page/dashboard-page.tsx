import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  ActionCenterItem,
  DashboardBundle,
  LowInventoryAlert
} from '../../../../core/models/dashboard.model';
import { useAuth } from '../../../../core/services/auth.service';
import { dashboardService } from '../../../../core/services/dashboard.service';
import { useDealerships } from '../../../../core/services/dealership.service';
import { useRealtimeEvent } from '../../../../core/services/realtime.service';
import { AgingSeverityBadgeComponent } from '../../../../shared/components/aging-severity-badge/aging-severity-badge';
import { DemandBadgeComponent } from '../../../../shared/components/demand-badge/demand-badge';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { formatCurrency } from '../../../../shared/pipes/format-currency.pipe';
import { ActionCenterSectionComponent } from '../../components/action-center-section/action-center-section';
import { InventoryChartsComponent } from '../../components/inventory-charts/inventory-charts';
import { LowInventoryAlertsComponent } from '../../components/low-inventory-alerts/low-inventory-alerts';
import { QuickStatsSectionComponent } from '../../components/quick-stats-section/quick-stats-section';
import { SellingVehiclesTrendComponent } from '../../components/selling-vehicles-trend/selling-vehicles-trend';
import './dashboard-page.scss';

export function DashboardPageComponent() {
  const { isManager } = useAuth();
  const {
    dealerships,
    selectedDealershipId,
    isLoading: isDealerLoading,
    error: dealerError,
    loadDealerships
  } = useDealerships();
  const navigate = useNavigate();

  const [bundle, setBundle] = useState<DashboardBundle | null>(null);
  const [alerts, setAlerts] = useState<readonly LowInventoryAlert[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const bundleReqIdRef = useRef(0);
  const alertsReqIdRef = useRef(0);

  const fetchDashboardForDealership = useCallback((dealershipId: string, showLoading = true) => {
    const reqId = ++bundleReqIdRef.current;

    if (showLoading) {
      setLoading(true);
    }
    setError(null);

    dashboardService
      .getDashboardBundle(1, 10, dealershipId)
      .then(res => {
        if (bundleReqIdRef.current !== reqId) return;
        setBundle(res.data);
        setLoading(false);
      })
      .catch(err => {
        if (bundleReqIdRef.current !== reqId) return;
        setLoading(false);
        const errMsg =
          (err?.error as { message?: string } | null | undefined)?.message ||
          'Unable to connect to IID API server.';
        setError(errMsg);
      });
  }, []);

  const fetchAlertsForDealership = useCallback((dealershipId: string) => {
    const reqId = ++alertsReqIdRef.current;

    dashboardService
      .getLowInventoryAlerts(dealershipId)
      .then(res => {
        if (alertsReqIdRef.current !== reqId) return;
        setAlerts(res.data);
      })
      .catch(() => {
        // Low inventory alerts errors should not block dashboard rendering
      });
  }, []);

  const loadDashboard = useCallback(
    (showLoading = true) => {
      if (!selectedDealershipId) {
        loadDealerships();
        return;
      }
      fetchDashboardForDealership(selectedDealershipId, showLoading);
      fetchAlertsForDealership(selectedDealershipId);
    },
    [selectedDealershipId, loadDealerships, fetchDashboardForDealership, fetchAlertsForDealership]
  );

  // Ensure dealerships are requested on mount if empty
  useEffect(() => {
    if (dealerships.length === 0 && !isDealerLoading) {
      loadDealerships();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Re-trigger load when active dealership or dealership loading state changes
  useEffect(() => {
    if (selectedDealershipId) {
      fetchDashboardForDealership(selectedDealershipId, true);
      fetchAlertsForDealership(selectedDealershipId);

      return () => {
        bundleReqIdRef.current += 1;
        alertsReqIdRef.current += 1;
      };
    }

    if (isDealerLoading) {
      setLoading(true);
      setError(null);
    } else if (dealerError) {
      setLoading(false);
      setError(`Failed to load dealerships: ${dealerError}`);
    } else if (dealerships.length === 0) {
      setLoading(false);
      setError('No dealership available. Please create or assign a dealership first.');
    }

    return () => {
      bundleReqIdRef.current += 1;
      alertsReqIdRef.current += 1;
    };
  }, [
    selectedDealershipId,
    isDealerLoading,
    dealerError,
    dealerships.length,
    fetchDashboardForDealership,
    fetchAlertsForDealership
  ]);

  const handleRealtimeRefresh = () => {
    if (selectedDealershipId) {
      fetchDashboardForDealership(selectedDealershipId, false);
      fetchAlertsForDealership(selectedDealershipId);
    }
  };

  useRealtimeEvent('inventoryChanged', handleRealtimeRefresh);
  useRealtimeEvent('summaryUpdated', handleRealtimeRefresh);
  useRealtimeEvent('alertsUpdated', handleRealtimeRefresh);

  const handleActionItem = (item: ActionCenterItem) => {
    if (item.vehicleId) {
      navigate(`/vehicles?vin=${encodeURIComponent(item.vehicleId)}`);
    } else {
      navigate('/vehicles');
    }
  };

  return (
    <div className="iid-dashboard-page dashboard-page">
      {/* Page Header */}
      <div className="page-topbar">
        <div className="topbar-title">
          <h2>Inventory Overview</h2>
          <p>Live inventory distribution, capital valuation, aging velocity &amp; predictive insights.</p>
        </div>

        <div className="topbar-actions">
          <button
            type="button"
            className="iid-btn iid-btn-secondary iid-btn-sm"
            onClick={() => loadDashboard(true)}
          >
            <span className="material-symbols-outlined">refresh</span>
            <span>Refresh Metrics</span>
          </button>

          <Link to="/vehicles" className="iid-btn iid-btn-primary iid-btn-sm">
            <span className="material-symbols-outlined">directions_car</span>
            <span>Manage Inventory</span>
          </Link>
        </div>
      </div>

      {loading ? (
        <LoadingSpinnerComponent
          message="Computing live inventory telemetry & analytics..."
          size="lg"
        />
      ) : error ? (
        <div className="error-banner iid-card">
          <div className="error-left">
            <span className="material-symbols-outlined error-icon">error</span>
            <div>
              <h4>Connection Error</h4>
              <p>{error}</p>
            </div>
          </div>
          <button
            type="button"
            className="iid-btn iid-btn-primary iid-btn-sm"
            onClick={() => loadDashboard(true)}
          >
            Retry Connection
          </button>
        </div>
      ) : bundle ? (
        <>
          {/* Quick Stats Metric Row */}
          <QuickStatsSectionComponent
            summary={bundle.summary}
            quickStats={bundle.quickStats}
            isManager={isManager}
          />

          {/* Middle Split Row: Selling Vehicles Trend & Action Center Recommendations */}
          <div className="dashboard-split-row">
            <SellingVehiclesTrendComponent monthlySales={bundle.charts.monthlySales} />

            <ActionCenterSectionComponent
              items={bundle.actionCenter}
              onActionClick={handleActionItem}
            />
          </div>

          {/* Distribution Charts Matrix (Aging Histogram, Powertrain, Status Breakdown) */}
          <InventoryChartsComponent charts={bundle.charts} />

          {/* Low Inventory Model Alerts */}
          <LowInventoryAlertsComponent alerts={alerts} />

          {/* Quick Inventory Recent Stock Table */}
          <div className="recent-stock-card iid-card">
            <div className="card-header">
              <div className="header-left">
                <span className="material-symbols-outlined icon">minor_crash</span>
                <h3>Recent Inventory Inflow</h3>
              </div>
              <Link to="/vehicles" className="view-all-link">
                <span>View All {bundle.inventory.total} Vehicles</span>
                <span className="material-symbols-outlined">arrow_forward</span>
              </Link>
            </div>

            <div className="table-container">
              <table className="inventory-table">
                <thead>
                  <tr>
                    <th>Vehicle</th>
                    <th>VIN / Stock</th>
                    <th>Fuel</th>
                    <th>Asking Price</th>
                    <th>Status</th>
                    <th>Days On Lot</th>
                    <th>Aging</th>
                    <th>Demand</th>
                  </tr>
                </thead>
                <tbody>
                  {bundle.inventory.items.map(veh => (
                    <tr key={veh.id} className="table-row">
                      <td className="col-vehicle">
                        <div className="veh-title">
                          {veh.year} {veh.make} {veh.model}
                        </div>
                        <div className="veh-meta">
                          {veh.color} &bull; {veh.mileage.toLocaleString()} miles
                        </div>
                      </td>
                      <td className="col-vin">
                        <div className="vin-code">{veh.vin}</div>
                        <div className="stock-badge">{veh.stockNumber}</div>
                      </td>
                      <td>
                        <span className="fuel-tag">{veh.fuelType}</span>
                      </td>
                      <td className="col-price">{formatCurrency(veh.askingPriceAmount)}</td>
                      <td>
                        <StatusBadgeComponent status={veh.status} />
                      </td>
                      <td>
                        <span className="days-cell">{veh.daysInInventory}d</span>
                      </td>
                      <td>
                        <AgingSeverityBadgeComponent severity={veh.agingSeverity} />
                      </td>
                      <td>
                        <DemandBadgeComponent level={veh.demandLevel} score={veh.demandScore} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </>
      ) : null}
    </div>
  );
}
