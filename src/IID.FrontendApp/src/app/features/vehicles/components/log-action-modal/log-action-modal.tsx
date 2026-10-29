import { FormEvent, useCallback, useEffect, useState } from 'react';
import {
  LogVehicleActionRequest,
  Vehicle,
  VehicleAction,
  VehicleActionType
} from '../../../../core/models/vehicle.model';
import { inventoryService } from '../../../../core/services/inventory.service';
import { useRealtimeEvent } from '../../../../core/services/realtime.service';
import { useToast } from '../../../../core/services/toast.service';
import { extractErrorMessage } from '../../../../core/utils/error-extractor';
import './log-action-modal.scss';

export interface LogActionModalProps {
  vehicle: Vehicle;
  onClose: () => void;
  onActionLogged?: (id: string) => void;
}

const ACTION_TYPES: { type: VehicleActionType; label: string; desc: string }[] = [
  { type: 'PriceReductionPlanned', label: 'Price Reduction Planned', desc: 'Flag price cut for upcoming adjustment' },
  { type: 'PriceReductionExecuted', label: 'Price Reduction Executed', desc: 'Applied discount to asking price' },
  { type: 'TransferToWholesale', label: 'Transfer to Wholesale', desc: 'Route aging unit to wholesale broker' },
  { type: 'MarketingCampaign', label: 'Marketing Push Campaign', desc: 'Boost vehicle on digital ad channels' },
  { type: 'DealerAuction', label: 'Dealer Auction Placement', desc: 'List unit on dealer wholesale network' },
  { type: 'ManagerReview', label: 'Manager Review Required', desc: 'Inspection or appraisal assessment' },
  { type: 'Relist', label: 'Relist Vehicle Online', desc: 'Refresh listing photos and description' },
  { type: 'TransferDealership', label: 'Transfer Dealership Showroom', desc: 'Relocate vehicle to another dealership showroom' },
  { type: 'TradeInCustomer', label: 'Trade-in Appraisal', desc: 'Customer trade-in appraisal log' },
  { type: 'Other', label: 'Other Operational Action', desc: 'General operational activity log' }
];

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

export function LogActionModalComponent({ vehicle, onClose, onActionLogged }: LogActionModalProps) {
  const toast = useToast();

  const [actionType, setActionType] = useState<VehicleActionType>('PriceReductionPlanned');
  const [notes, setNotes] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const [actions, setActions] = useState<readonly VehicleAction[]>([]);
  const [isLoadingActions, setIsLoadingActions] = useState<boolean>(false);

  const loadActions = useCallback(() => {
    setIsLoadingActions(true);
    inventoryService
      .getVehicleActions(vehicle.id)
      .then(res => {
        setActions(res.data);
        setIsLoadingActions(false);
      })
      .catch(() => {
        setIsLoadingActions(false);
      });
  }, [vehicle.id]);

  useEffect(() => {
    loadActions();
  }, [loadActions]);

  useRealtimeEvent('vehicleActionLogged', action => {
    if (action.vehicleId === vehicle.id) {
      loadActions();
    }
  });

  function setNotePreset(text: string): void {
    setNotes(text);
  }

  function onSubmit(e: FormEvent<HTMLFormElement>): void {
    e.preventDefault();
    if (!actionType) return;

    setIsSubmitting(true);
    setErrorMessage(null);

    const req: LogVehicleActionRequest = {
      actionType,
      notes: notes.trim() || undefined
    };

    inventoryService
      .logVehicleAction(vehicle.id, req)
      .then(res => {
        setIsSubmitting(false);
        toast.success(
          'Action Recorded',
          `Logged "${actionType}" on ${vehicle.year} ${vehicle.make} ${vehicle.model}.`
        );
        setNotes('');
        onActionLogged?.(res.data.id);
        loadActions();
      })
      .catch((err: unknown) => {
        setIsSubmitting(false);
        const msg = extractErrorMessage(err, 'Failed to record action.');
        setErrorMessage(msg);
        toast.error('Action Failed', msg);
      });
  }

  return (
    <div className="iid-log-action-modal iid-modal-backdrop" onClick={onClose}>
      <div className="modal-card iid-card" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <div className="header-title">
            <span className="material-symbols-outlined icon">assignment_add</span>
            <h3>Log Action on Vehicle</h3>
          </div>
          <button type="button" className="close-btn" onClick={onClose}>
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        {/* Scrollable Modal Body */}
        <div className="modal-body">
          {/* Vehicle Target Header */}
          <div className="veh-summary-box">
            <div className="veh-title">
              {vehicle.year} {vehicle.make} {vehicle.model}
            </div>
            <div className="veh-meta">
              <span>
                VIN: <code>{vehicle.vin}</code>
              </span>{' '}
              &bull;{' '}
              <span>
                Stock: <code>{vehicle.stockNumber}</code>
              </span>{' '}
              &bull;{' '}
              <span>
                Lot Days: <strong>{vehicle.daysInInventory}d</strong>
              </span>
            </div>
          </div>

          {errorMessage && (
            <div className="error-banner">
              <span className="material-symbols-outlined">error</span>
              <span>{errorMessage}</span>
            </div>
          )}

          {/* Form Section */}
          <form onSubmit={onSubmit} className="modal-form">
            <div className="form-group">
              <label className="iid-form-label" htmlFor="actionType">
                Select Action Type *
              </label>
              <select
                id="actionType"
                className="iid-input iid-select"
                value={actionType}
                onChange={e => setActionType(e.target.value as VehicleActionType)}
              >
                {ACTION_TYPES.map(t => (
                  <option key={t.type} value={t.type}>
                    {t.label} — {t.desc}
                  </option>
                ))}
              </select>
            </div>

            {/* Quick Note Preset Chips */}
            <div className="presets-section">
              <span className="preset-label">Suggested Notes:</span>
              <div className="preset-chips">
                <button
                  type="button"
                  className="chip"
                  onClick={() =>
                    setNotePreset('Price reduced by 5% to stimulate dealer test drives.')
                  }
                >
                  Reduce 5%
                </button>
                <button
                  type="button"
                  className="chip"
                  onClick={() =>
                    setNotePreset(
                      'Transferred vehicle to wholesale channel due to 90+ days aging.'
                    )
                  }
                >
                  Wholesale transfer
                </button>
                <button
                  type="button"
                  className="chip"
                  onClick={() =>
                    setNotePreset(
                      'Launched digital campaign boost on social & aggregator sites.'
                    )
                  }
                >
                  Ad Campaign
                </button>
                <button
                  type="button"
                  className="chip"
                  onClick={() =>
                    setNotePreset('Manager review completed: mechanical condition verified.')
                  }
                >
                  Manager Review
                </button>
              </div>
            </div>

            <div className="form-group">
              <label className="iid-form-label" htmlFor="actionNotes">
                Action Details &amp; Notes
              </label>
              <textarea
                id="actionNotes"
                className="iid-input notes-textarea"
                rows={3}
                placeholder="Enter operational notes or reason for adjustment..."
                value={notes}
                onChange={e => setNotes(e.target.value)}
              />
            </div>

            <div className="form-submit-row">
              <button
                type="submit"
                className="iid-btn iid-btn-primary"
                disabled={isSubmitting || !actionType}
              >
                {isSubmitting ? (
                  <span>Recording Log...</span>
                ) : (
                  <>
                    <span className="material-symbols-outlined">save</span>
                    <span>Record Action</span>
                  </>
                )}
              </button>
            </div>
          </form>

          {/* Action History Section */}
          <div className="history-section">
            <div className="history-header">
              <div className="history-title">
                <span className="material-symbols-outlined">history</span>
                <h4>Action History</h4>
                <span className="count-badge">{actions.length}</span>
              </div>
              <button
                type="button"
                className="refresh-btn"
                onClick={loadActions}
                disabled={isLoadingActions}
                title="Reload action history"
              >
                <span className={`material-symbols-outlined${isLoadingActions ? ' spin' : ''}`}>
                  refresh
                </span>
              </button>
            </div>

            {isLoadingActions && actions.length === 0 ? (
              <div className="loading-state">
                <span className="material-symbols-outlined spin">progress_activity</span>
                <span>Loading logged actions...</span>
              </div>
            ) : actions.length === 0 ? (
              <div className="empty-state">
                <span className="material-symbols-outlined">assignment_late</span>
                <p>No previous actions logged for this vehicle yet.</p>
              </div>
            ) : (
              <div className="actions-list">
                {actions.map(item => (
                  <div key={item.id} className="action-item">
                    <div className="action-item-header">
                      <span className="action-badge" data-action={item.actionType}>
                        {item.actionType}
                      </span>
                      <span className="action-time">{formatActionDate(item.loggedAtUtc)}</span>
                    </div>
                    {item.notes && <div className="action-notes">{item.notes}</div>}
                    <div className="action-meta">
                      <span>
                        Logged by: <strong>{item.loggedByName || item.loggedByUserId}</strong>
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        <div className="modal-footer">
          <button
            type="button"
            className="iid-btn iid-btn-secondary"
            onClick={onClose}
            disabled={isSubmitting}
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
}
