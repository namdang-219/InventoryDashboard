import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Dealership } from '../../../../core/models/dealership.model';
import { TransferVehicleRequest, Vehicle } from '../../../../core/models/vehicle.model';
import { useDealerships } from '../../../../core/services/dealership.service';
import { inventoryService } from '../../../../core/services/inventory.service';
import { useToast } from '../../../../core/services/toast.service';
import { extractErrorMessage } from '../../../../core/utils/error-extractor';
import { formatCurrency } from '../../../../shared/pipes/format-currency.pipe';
import './transfer-dealership-modal.scss';

export interface TransferDealershipModalProps {
  vehicle: Vehicle;
  onClose: () => void;
  onTransferred?: (payload: { vehicleId: string; targetDealershipId: string }) => void;
}

export function TransferDealershipModalComponent({
  vehicle,
  onClose,
  onTransferred
}: TransferDealershipModalProps) {
  const dealershipService = useDealerships();
  const toast = useToast();

  const availableDealerships = useMemo(
    () => dealershipService.dealerships.filter(d => d.id !== vehicle.dealershipId),
    [dealershipService.dealerships, vehicle.dealershipId]
  );

  const currentDealershipName = useMemo(() => {
    if (vehicle.dealershipName) {
      return vehicle.dealershipName;
    }
    const match = dealershipService.dealerships.find(d => d.id === vehicle.dealershipId);
    return match ? `${match.name} (${match.city}, ${match.state})` : 'Current Showroom';
  }, [dealershipService.dealerships, vehicle.dealershipId, vehicle.dealershipName]);

  const [targetDealershipId, setTargetDealershipId] = useState<string>(
    () => availableDealerships[0]?.id ?? ''
  );
  const [targetTouched, setTargetTouched] = useState<boolean>(false);
  const [notes, setNotes] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (
      availableDealerships.length > 0 &&
      (!targetDealershipId || targetDealershipId === vehicle.dealershipId)
    ) {
      setTargetDealershipId(availableDealerships[0].id);
    }
  }, [availableDealerships, targetDealershipId, vehicle.dealershipId]);

  const selectedTargetDealership = useMemo<Dealership | null>(() => {
    if (!targetDealershipId) return null;
    return dealershipService.dealerships.find(d => d.id === targetDealershipId) ?? null;
  }, [dealershipService.dealerships, targetDealershipId]);

  const isTargetValid = targetDealershipId.length > 0;

  function onSubmit(e: FormEvent<HTMLFormElement>): void {
    e.preventDefault();
    if (isSubmitting) return;

    if (!isTargetValid) {
      setTargetTouched(true);
      return;
    }

    if (!targetDealershipId || targetDealershipId === vehicle.dealershipId) {
      setErrorMessage('Please select a valid destination dealership different from current showroom.');
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);

    const req: TransferVehicleRequest = {
      targetDealershipId,
      notes: notes.trim() || undefined
    };

    const targetDealerName = selectedTargetDealership?.name ?? 'target dealership';

    inventoryService
      .transferDealership(vehicle.id, req)
      .then(() => {
        setIsSubmitting(false);
        toast.success(
          'Vehicle Transferred!',
          `${vehicle.year} ${vehicle.make} ${vehicle.model} transferred to ${targetDealerName}.`
        );
        onTransferred?.({
          vehicleId: vehicle.id,
          targetDealershipId
        });
        onClose();
      })
      .catch((err: unknown) => {
        setIsSubmitting(false);
        const msg = extractErrorMessage(err, 'Failed to transfer vehicle dealership.');
        setErrorMessage(msg);
        toast.error('Transfer Failed', msg);
      });
  }

  return (
    <div className="iid-transfer-dealership-modal iid-modal-backdrop" onClick={onClose}>
      <div className="modal-card iid-card" onClick={e => e.stopPropagation()}>
        {/* Modal Header */}
        <div className="modal-header">
          <div className="header-title">
            <span className="material-symbols-outlined icon">swap_horiz</span>
            <h3>Transfer Vehicle Showroom</h3>
          </div>
          <button type="button" className="close-btn" onClick={onClose} title="Close dialog">
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        {/* Vehicle Details Summary */}
        <div className="veh-summary-box">
          <div className="veh-info">
            <h4>
              {vehicle.year} {vehicle.make} {vehicle.model}
            </h4>
            <span className="vin-label">
              VIN: {vehicle.vin} &bull; Stock: {vehicle.stockNumber}
            </span>
          </div>
          <div className="veh-metrics">
            <div className="metric-item">
              <span className="label">Asking Price:</span>
              <span className="val font-accent">{formatCurrency(vehicle.askingPriceAmount)}</span>
            </div>
            <div className="metric-item">
              <span className="label">Lot Aging:</span>
              <span className={`val${vehicle.isAging ? ' aging-danger' : ''}`}>
                {vehicle.daysInInventory} days
              </span>
            </div>
          </div>
        </div>

        {errorMessage && (
          <div className="error-banner">
            <span className="material-symbols-outlined">error</span>
            <span>{errorMessage}</span>
          </div>
        )}

        <form onSubmit={onSubmit} className="modal-form">
          {/* Transfer Route Visualization */}
          <div className="transfer-route-card">
            <div className="route-node source-node">
              <div className="node-badge">FROM</div>
              <div className="node-content">
                <span className="material-symbols-outlined node-icon">storefront</span>
                <div className="node-text">
                  <span className="node-label">Current Dealership</span>
                  <strong className="node-name">{currentDealershipName}</strong>
                </div>
              </div>
            </div>

            <div className="route-arrow">
              <span className="material-symbols-outlined">arrow_forward</span>
            </div>

            <div className="route-node dest-node">
              <div className="node-badge">TO</div>
              <div className="node-content">
                <span className="material-symbols-outlined node-icon accent-icon">location_on</span>
                <div className="node-text">
                  <span className="node-label">Destination Dealership *</span>
                  <select
                    id="targetDealershipId"
                    className="iid-input iid-select target-select"
                    value={targetDealershipId}
                    onChange={e => setTargetDealershipId(e.target.value)}
                    onBlur={() => setTargetTouched(true)}
                  >
                    {availableDealerships.length === 0 && (
                      <option value="" disabled>
                        No other dealerships available
                      </option>
                    )}
                    {availableDealerships.map(d => (
                      <option key={d.id} value={d.id}>
                        {d.name} &bull; {d.city}, {d.state}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
            </div>
          </div>

          {targetTouched && !isTargetValid && (
            <span className="error-text">Please choose a destination dealership.</span>
          )}

          {/* Transfer Reason / Notes */}
          <div className="form-group notes-group">
            <label className="iid-form-label" htmlFor="transferNotes">
              Transfer Reason / Operational Notes (Optional)
            </label>
            <textarea
              id="transferNotes"
              className="iid-input iid-textarea"
              rows={3}
              placeholder="E.g., High buyer demand in target market, rebalancing inventory lot density, dealer network trade..."
              value={notes}
              onChange={e => setNotes(e.target.value)}
            />
          </div>

          {/* Realtime Policy Notice */}
          <div className="policy-note">
            <span className="material-symbols-outlined note-icon">sync</span>
            <div>
              <span>
                This action immediately transfers inventory ownership, logs an audit entry, and
                re-routes SignalR telemetry streams between showrooms.
              </span>
            </div>
          </div>

          {/* Action Footer */}
          <div className="modal-footer">
            <button
              type="button"
              className="iid-btn iid-btn-secondary"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="iid-btn iid-btn-primary"
              disabled={isSubmitting || !isTargetValid || availableDealerships.length === 0}
            >
              {isSubmitting ? (
                <>
                  <span className="material-symbols-outlined spin-icon">progress_activity</span>
                  <span>Transferring...</span>
                </>
              ) : (
                <>
                  <span className="material-symbols-outlined">swap_horiz</span>
                  <span>Confirm Transfer</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
