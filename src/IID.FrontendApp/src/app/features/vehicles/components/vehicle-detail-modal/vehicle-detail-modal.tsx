import { useEffect, useState } from 'react';
import { Vehicle, VehicleAction } from '../../../../core/models/vehicle.model';
import { inventoryService } from '../../../../core/services/inventory.service';
import { AgingSeverityBadgeComponent } from '../../../../shared/components/aging-severity-badge/aging-severity-badge';
import { DemandBadgeComponent } from '../../../../shared/components/demand-badge/demand-badge';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { formatCurrency } from '../../../../shared/pipes/format-currency.pipe';
import './vehicle-detail-modal.scss';

export interface VehicleDetailModalProps {
  vehicle: Vehicle;
  isManager?: boolean;
  canMarkSold?: boolean;
  highlightActionId?: string | null;
  onClose: () => void;
  onEditVehicle?: (v: Vehicle) => void;
  onMarkSold?: (v: Vehicle) => void;
  onLogAction?: (v: Vehicle) => void;
  onTransferDealership?: (v: Vehicle) => void;
}

function formatActionDate(utcString: string): string {
  const d = new Date(utcString);
  if (Number.isNaN(d.getTime())) return utcString;
  return d.toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  });
}

export function VehicleDetailModalComponent({
  vehicle,
  isManager = false,
  canMarkSold = false,
  highlightActionId = null,
  onClose,
  onEditVehicle,
  onMarkSold,
  onLogAction,
  onTransferDealership
}: VehicleDetailModalProps) {
  const [actions, setActions] = useState<readonly VehicleAction[]>([]);
  const [isLoadingActions, setIsLoadingActions] = useState<boolean>(false);

  useEffect(() => {
    let cancelled = false;
    setIsLoadingActions(true);
    inventoryService
      .getVehicleActions(vehicle.id)
      .then(res => {
        if (cancelled) return;
        setActions(res.data);
        setIsLoadingActions(false);
      })
      .catch(() => {
        if (cancelled) return;
        setIsLoadingActions(false);
      });

    return () => {
      cancelled = true;
    };
  }, [vehicle.id]);

  useEffect(() => {
    if (!highlightActionId || actions.length === 0) return;

    const timer = setTimeout(() => {
      const el = document.getElementById(`action-card-${highlightActionId.toLowerCase()}`);
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
        el.focus({ preventScroll: true });
      }
    }, 150);

    return () => clearTimeout(timer);
  }, [highlightActionId, actions]);

  function isActionHighlighted(actionId: string): boolean {
    if (!highlightActionId) return false;
    return actionId.toLowerCase() === highlightActionId.toLowerCase();
  }

  return (
    <div className="iid-vehicle-detail-modal iid-modal-backdrop" onClick={onClose}>
      <div className="modal-card iid-card" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <div className="header-left">
            <span className="material-symbols-outlined icon">directions_car</span>
            <div>
              <h3>
                {vehicle.year} {vehicle.make} {vehicle.model}
              </h3>
              <span className="sub-id">
                VIN: {vehicle.vin} &bull; Stock: {vehicle.stockNumber}
              </span>
            </div>
          </div>
          <div className="header-actions">
            <button type="button" className="close-btn" onClick={onClose}>
              <span className="material-symbols-outlined">close</span>
            </button>
          </div>
        </div>

        {/* Scrollable Modal Body */}
        <div className="modal-body">
          {/* Status & Analytics Pills */}
          <div className="pills-strip">
            <StatusBadgeComponent status={vehicle.status} />
            <AgingSeverityBadgeComponent
              severity={vehicle.agingSeverity}
              days={vehicle.daysInInventory}
            />
            <DemandBadgeComponent level={vehicle.demandLevel} score={vehicle.demandScore} />
            <span className="fuel-chip">{vehicle.fuelType}</span>
          </div>

          {/* Pricing & Profit Matrix */}
          <div className="financials-card">
            {isManager && (
              <div className="fin-item">
                <span className="fin-label">Purchase Price</span>
                <span className="fin-val">{formatCurrency(vehicle.purchasePriceAmount)}</span>
              </div>
            )}
            <div className="fin-item">
              <span className="fin-label">Asking Price</span>
              <span className="fin-val font-accent">
                {formatCurrency(vehicle.askingPriceAmount)}
              </span>
            </div>
            {vehicle.soldPriceAmount ? (
              <>
                <div className="fin-item">
                  <span className="fin-label">Sold Price</span>
                  <span className="fin-val font-sold">
                    {formatCurrency(vehicle.soldPriceAmount)}
                  </span>
                </div>
                {isManager && (
                  <div className="fin-item">
                    <span className="fin-label">Realized Profit</span>
                    <span className="fin-val font-profit">
                      {formatCurrency(vehicle.grossProfitAmount)} ({vehicle.grossMarginPercent}%)
                    </span>
                  </div>
                )}
              </>
            ) : isManager ? (
              <div className="fin-item">
                <span className="fin-label">Target Markup</span>
                <span className="fin-val font-profit">
                  {formatCurrency(vehicle.askingPriceAmount - vehicle.purchasePriceAmount)}
                </span>
              </div>
            ) : null}
          </div>

          {/* Specification Grid */}
          <div className="specs-grid">
            <div className="spec-item">
              <span className="spec-label">Exterior Color</span>
              <span className="spec-val">{vehicle.color}</span>
            </div>
            <div className="spec-item">
              <span className="spec-label">Odometer</span>
              <span className="spec-val">{vehicle.mileage.toLocaleString()} miles</span>
            </div>
            <div className="spec-item">
              <span className="spec-label">Days On Lot</span>
              <span className="spec-val">{vehicle.daysInInventory} days</span>
            </div>
            <div className="spec-item">
              <span className="spec-label">Date Inflow</span>
              <span className="spec-val">{vehicle.dateAddedToInventoryUtc.substring(0, 10)}</span>
            </div>
            <div className="spec-item">
              <span className="spec-label">Demand Score</span>
              <span className="spec-val">{vehicle.demandScore}/100</span>
            </div>
            {vehicle.dealershipName && (
              <div className="spec-item">
                <span className="spec-label">Dealership Showroom</span>
                <span className="spec-val font-accent">{vehicle.dealershipName}</span>
              </div>
            )}
          </div>

          {/* Action History Section */}
          <div className="detail-history-section">
            <div className="section-title">
              <span className="material-symbols-outlined">history</span>
              <h4>Action History ({actions.length})</h4>
            </div>
            {isLoadingActions ? (
              <div className="history-empty">Loading logged actions...</div>
            ) : actions.length === 0 ? (
              <div className="history-empty">No actions logged for this vehicle yet.</div>
            ) : (
              <div className="detail-actions-list">
                {actions.map(action => {
                  const highlighted = isActionHighlighted(action.id);
                  return (
                    <div
                      key={action.id}
                      id={`action-card-${action.id.toLowerCase()}`}
                      className={`detail-action-card${highlighted ? ' highlighted-action' : ''}`}
                      tabIndex={-1}
                    >
                      <div className="action-top">
                        <div className="action-tag-group">
                          <span className="action-tag">{action.actionType}</span>
                          {highlighted && (
                            <span className="action-highlight-badge">
                              <span className="material-symbols-outlined">center_focus_strong</span>
                              <span>Highlighted Event</span>
                            </span>
                          )}
                        </div>
                        <span className="action-time">{formatActionDate(action.loggedAtUtc)}</span>
                      </div>
                      {action.notes && <div className="action-note">{action.notes}</div>}
                      <div className="action-user">
                        Logged by: <strong>{action.loggedByName || action.loggedByUserId}</strong>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>

        {/* Concurrency Footer Actions */}
        <div className="modal-footer">
          <button
            type="button"
            className="iid-btn iid-btn-secondary close-btn-footer"
            onClick={onClose}
          >
            Close
          </button>

          {isManager && vehicle.status !== 'Sold' && (
            <>
              <button
                type="button"
                className="iid-btn iid-btn-secondary"
                onClick={() => onTransferDealership?.(vehicle)}
              >
                <span className="material-symbols-outlined">swap_horiz</span>
                <span>Transfer Dealership</span>
              </button>

              <button
                type="button"
                className="iid-btn iid-btn-secondary"
                onClick={() => onEditVehicle?.(vehicle)}
              >
                <span className="material-symbols-outlined">edit</span>
                <span>Edit Vehicle</span>
              </button>

              <button
                type="button"
                className="iid-btn iid-btn-secondary"
                onClick={() => onLogAction?.(vehicle)}
              >
                <span className="material-symbols-outlined">assignment_add</span>
                <span>Log Action</span>
              </button>
            </>
          )}

          {canMarkSold && vehicle.status !== 'Sold' && (
            <button
              type="button"
              className="iid-btn iid-btn-primary"
              onClick={() => onMarkSold?.(vehicle)}
            >
              <span className="material-symbols-outlined">monetization_on</span>
              <span>Mark as Sold</span>
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
