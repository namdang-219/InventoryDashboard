import { FormEvent, useEffect, useMemo, useState } from 'react';
import { MarkVehicleSoldRequest, Vehicle } from '../../../../core/models/vehicle.model';
import { useAuth } from '../../../../core/services/auth.service';
import { inventoryService } from '../../../../core/services/inventory.service';
import { useToast } from '../../../../core/services/toast.service';
import { extractErrorMessage } from '../../../../core/utils/error-extractor';
import { formatCurrency } from '../../../../shared/pipes/format-currency.pipe';
import './mark-sold-modal.scss';

export interface MarkSoldModalProps {
  vehicle: Vehicle;
  isManager?: boolean;
  onClose: () => void;
  onSoldSuccess?: (id: string) => void;
}

export function MarkSoldModalComponent({
  vehicle,
  isManager,
  onClose,
  onSoldSuccess
}: MarkSoldModalProps) {
  const auth = useAuth();
  const toast = useToast();

  const effectiveIsManager = isManager ?? auth.isManager;

  const [soldPrice, setSoldPrice] = useState<number | ''>(vehicle.askingPriceAmount);
  const [soldPriceTouched, setSoldPriceTouched] = useState<boolean>(false);
  const [soldPriceCurrency] = useState<string>('USD');
  const [soldAtDate, setSoldAtDate] = useState<string>(() =>
    new Date().toISOString().substring(0, 10)
  );

  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    setSoldPrice(vehicle.askingPriceAmount);
    setSoldPriceTouched(false);
    setErrorMessage(null);
  }, [vehicle]);

  const numericSoldPrice = typeof soldPrice === 'number' && !Number.isNaN(soldPrice) ? soldPrice : 0;

  const estimatedProfit = useMemo(() => {
    return numericSoldPrice - vehicle.purchasePriceAmount;
  }, [numericSoldPrice, vehicle.purchasePriceAmount]);

  const estimatedMargin = useMemo(() => {
    const purchase = vehicle.purchasePriceAmount;
    if (!purchase || !numericSoldPrice) return 0;
    return Math.round(((numericSoldPrice - purchase) / purchase) * 100);
  }, [numericSoldPrice, vehicle.purchasePriceAmount]);

  const isSoldPriceValid =
    soldPrice !== '' && !Number.isNaN(soldPrice) && soldPrice >= 1;
  const isFormValid = isSoldPriceValid && soldAtDate.length > 0;

  function onSubmit(e: FormEvent<HTMLFormElement>): void {
    e.preventDefault();
    if (!isFormValid) {
      setSoldPriceTouched(true);
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);

    const req: MarkVehicleSoldRequest = {
      soldPrice: Number(soldPrice),
      soldPriceCurrency,
      soldAtUtc: new Date(soldAtDate).toISOString(),
      rowVersion: vehicle.rowVersion
    };

    inventoryService
      .markVehicleSold(vehicle.id, req)
      .then(() => {
        setIsSubmitting(false);
        toast.success(
          'Vehicle Sold!',
          `${vehicle.year} ${vehicle.make} ${vehicle.model} marked as Sold.`
        );
        onSoldSuccess?.(vehicle.id);
        onClose();
      })
      .catch((err: unknown) => {
        setIsSubmitting(false);
        const status = (err as { status?: number } | null)?.status;
        const msg =
          status === 409
            ? 'Concurrency Conflict: This vehicle was modified by another transaction. Please reload the vehicle to obtain the latest revision.'
            : extractErrorMessage(err, 'Failed to mark vehicle as sold.');
        setErrorMessage(msg);
        toast.error('Sale Failed', msg);
      });
  }

  return (
    <div className="iid-mark-sold-modal iid-modal-backdrop" onClick={onClose}>
      <div className="modal-card iid-card" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <div className="header-title">
            <span className="material-symbols-outlined icon">monetization_on</span>
            <h3>Mark Vehicle As Sold</h3>
          </div>
          <button type="button" className="close-btn" onClick={onClose}>
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        {/* Vehicle Summary Header */}
        <div className="veh-summary-box">
          <div className="veh-info">
            <h4>
              {vehicle.year} {vehicle.make} {vehicle.model}
            </h4>
            <span className="vin-label">
              VIN: {vehicle.vin} &bull; Stock: {vehicle.stockNumber}
            </span>
          </div>
          <div className="price-tags">
            {effectiveIsManager && (
              <div className="price-item">
                <span className="label">Purchase Cost:</span>
                <span className="val">{formatCurrency(vehicle.purchasePriceAmount)}</span>
              </div>
            )}
            <div className="price-item">
              <span className="label">Asking Price:</span>
              <span className="val font-bold">{formatCurrency(vehicle.askingPriceAmount)}</span>
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
          <div className="form-row two-col">
            <div className="form-group">
              <label className="iid-form-label" htmlFor="soldPrice">
                Final Sold Price ($) *
              </label>
              <input
                id="soldPrice"
                type="number"
                className="iid-input font-mono"
                value={soldPrice}
                onChange={e => setSoldPrice(e.target.value === '' ? '' : Number(e.target.value))}
                onBlur={() => setSoldPriceTouched(true)}
              />
              {soldPriceTouched && !isSoldPriceValid && (
                <span className="error-text">Valid sold price is required</span>
              )}
            </div>

            <div className="form-group">
              <label className="iid-form-label" htmlFor="soldDate">
                Sold Date *
              </label>
              <input
                id="soldDate"
                type="date"
                className="iid-input font-mono"
                value={soldAtDate}
                onChange={e => setSoldAtDate(e.target.value)}
              />
            </div>
          </div>

          {/* Live Profit Telemetry Projection (Manager Only) */}
          {effectiveIsManager && (
            <div className="profit-projection-box">
              <div className="projection-item">
                <span className="proj-label">Projected Gross Profit</span>
                <span
                  className={`proj-val ${estimatedProfit >= 0 ? 'profit-positive' : 'profit-negative'}`}
                >
                  {formatCurrency(estimatedProfit)}
                </span>
              </div>
              <div className="projection-item">
                <span className="proj-label">Profit Margin</span>
                <span
                  className={`proj-val ${estimatedMargin >= 0 ? 'profit-positive' : 'profit-negative'}`}
                >
                  {estimatedMargin}%
                </span>
              </div>
            </div>
          )}

          <div className="concurrency-note">
            <span className="material-symbols-outlined icon">verified_user</span>
            <span>Protected with Optimistic Concurrency Control (ETag Revision).</span>
          </div>

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
              disabled={isSubmitting || !isFormValid}
            >
              {isSubmitting ? (
                <span>Recording Sale...</span>
              ) : (
                <>
                  <span className="material-symbols-outlined">check_circle</span>
                  <span>Confirm Sale</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
