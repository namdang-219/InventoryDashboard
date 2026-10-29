import { FormEvent, useEffect, useState } from 'react';
import { FuelType, UpdateVehicleRequest, Vehicle, VehicleStatus } from '../../../../core/models/vehicle.model';
import { inventoryService } from '../../../../core/services/inventory.service';
import { useToast } from '../../../../core/services/toast.service';
import { extractErrorMessage } from '../../../../core/utils/error-extractor';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import './edit-vehicle-modal.scss';

export interface EditVehicleModalProps {
  vehicle: Vehicle;
  onClose: () => void;
  onUpdated?: () => void;
}

export function EditVehicleModalComponent({ vehicle, onClose, onUpdated }: EditVehicleModalProps) {
  const toast = useToast();
  const currentYear = new Date().getFullYear();

  const [make, setMake] = useState<string>(vehicle.make);
  const [model, setModel] = useState<string>(vehicle.model);
  const [year, setYear] = useState<number | ''>(vehicle.year);
  const [color, setColor] = useState<string>(vehicle.color);
  const [mileage, setMileage] = useState<number | ''>(vehicle.mileage);
  const [fuelType, setFuelType] = useState<FuelType>(vehicle.fuelType);
  const [purchasePrice, setPurchasePrice] = useState<number | ''>(vehicle.purchasePriceAmount);
  const [askingPrice, setAskingPrice] = useState<number | ''>(vehicle.askingPriceAmount);
  const [status, setStatus] = useState<VehicleStatus>(
    vehicle.status === 'Sold' ? 'Available' : vehicle.status
  );

  const [touched, setTouched] = useState<{
    make?: boolean;
    model?: boolean;
    year?: boolean;
    color?: boolean;
    mileage?: boolean;
    purchasePrice?: boolean;
    askingPrice?: boolean;
  }>({});

  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    setMake(vehicle.make);
    setModel(vehicle.model);
    setYear(vehicle.year);
    setColor(vehicle.color);
    setMileage(vehicle.mileage);
    setFuelType(vehicle.fuelType);
    setPurchasePrice(vehicle.purchasePriceAmount);
    setAskingPrice(vehicle.askingPriceAmount);
    setStatus(vehicle.status === 'Sold' ? 'Available' : vehicle.status);
    setTouched({});
    setErrorMessage(null);
  }, [vehicle]);

  const isMakeValid = make.trim().length > 0 && make.length <= 50;
  const isModelValid = model.trim().length > 0 && model.length <= 50;
  const isYearValid =
    year !== '' && !Number.isNaN(year) && year >= 1980 && year <= currentYear + 1;
  const isColorValid = color.trim().length > 0 && color.length <= 30;
  const isMileageValid = mileage !== '' && !Number.isNaN(mileage) && mileage >= 0;
  const isPurchasePriceValid =
    purchasePrice !== '' && !Number.isNaN(purchasePrice) && purchasePrice >= 1;
  const isAskingPriceValid = askingPrice !== '' && !Number.isNaN(askingPrice) && askingPrice >= 1;

  const isFormValid =
    isMakeValid &&
    isModelValid &&
    isYearValid &&
    isColorValid &&
    isMileageValid &&
    Boolean(fuelType) &&
    isPurchasePriceValid &&
    isAskingPriceValid &&
    Boolean(status);

  function markAllTouched(): void {
    setTouched({
      make: true,
      model: true,
      year: true,
      color: true,
      mileage: true,
      purchasePrice: true,
      askingPrice: true
    });
  }

  function onSubmit(e: FormEvent<HTMLFormElement>): void {
    e.preventDefault();
    if (!isFormValid) {
      markAllTouched();
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);

    const req: UpdateVehicleRequest = {
      make: make.trim(),
      model: model.trim(),
      year: Number(year),
      color: color.trim(),
      mileage: Number(mileage),
      fuelType,
      purchasePrice: Number(purchasePrice),
      askingPrice: Number(askingPrice),
      status,
      rowVersion: vehicle.rowVersion
    };

    inventoryService
      .updateVehicle(vehicle.id, req)
      .then(() => {
        setIsSubmitting(false);
        toast.success(
          'Vehicle Updated',
          `${req.year} ${req.make} ${req.model} details updated successfully.`
        );
        onUpdated?.();
        onClose();
      })
      .catch((err: unknown) => {
        setIsSubmitting(false);
        const msg = extractErrorMessage(err, 'Failed to update vehicle.');
        setErrorMessage(msg);
        toast.error('Failed to Update Vehicle', msg);
      });
  }

  return (
    <div className="iid-edit-vehicle-modal iid-modal-backdrop" onClick={onClose}>
      <div className="modal-card iid-card" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <div className="header-title">
            <span className="material-symbols-outlined icon">edit</span>
            <div>
              <h3>Edit Vehicle</h3>
              <span className="sub-id">
                VIN: {vehicle.vin} &bull; Stock: {vehicle.stockNumber}
              </span>
            </div>
          </div>
          <button type="button" className="close-btn" onClick={onClose} disabled={isSubmitting}>
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        <form onSubmit={onSubmit} className="modal-form-wrap">
          <div className="modal-body">
            {/* Vehicle Identification Info Banner */}
            <div className="vehicle-info-bar">
              <div className="info-block">
                <span className="label">VIN</span>
                <span className="val font-mono">{vehicle.vin}</span>
              </div>
              <div className="info-block">
                <span className="label">Stock Number</span>
                <span className="val font-mono">{vehicle.stockNumber}</span>
              </div>
              <div className="info-block">
                <span className="label">Days on Lot</span>
                <span className="val font-accent">{vehicle.daysInInventory} days</span>
              </div>
              <div className="info-block">
                <span className="label">Current Status</span>
                <div className="val">
                  <StatusBadgeComponent status={vehicle.status} />
                </div>
              </div>
            </div>

            {errorMessage && (
              <div className="error-banner">
                <span className="material-symbols-outlined">error</span>
                <span>{errorMessage}</span>
              </div>
            )}

            <div className="form-row three-col">
              <div className="form-group">
                <label className="iid-form-label" htmlFor="editMake">
                  Make *
                </label>
                <input
                  id="editMake"
                  type="text"
                  className="iid-input"
                  placeholder="Tesla"
                  value={make}
                  onChange={e => setMake(e.target.value)}
                  onBlur={() => setTouched(prev => ({ ...prev, make: true }))}
                />
                {touched.make && !isMakeValid && (
                  <span className="error-text">Make is required</span>
                )}
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="editModel">
                  Model *
                </label>
                <input
                  id="editModel"
                  type="text"
                  className="iid-input"
                  placeholder="Model 3"
                  value={model}
                  onChange={e => setModel(e.target.value)}
                  onBlur={() => setTouched(prev => ({ ...prev, model: true }))}
                />
                {touched.model && !isModelValid && (
                  <span className="error-text">Model is required</span>
                )}
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="editYear">
                  Year *
                </label>
                <input
                  id="editYear"
                  type="number"
                  className="iid-input font-mono"
                  value={year}
                  onChange={e => setYear(e.target.value === '' ? '' : Number(e.target.value))}
                  onBlur={() => setTouched(prev => ({ ...prev, year: true }))}
                />
                {touched.year && !isYearValid && (
                  <span className="error-text">Valid year required (1980–present)</span>
                )}
              </div>
            </div>

            <div className="form-row three-col">
              <div className="form-group">
                <label className="iid-form-label" htmlFor="editColor">
                  Color *
                </label>
                <input
                  id="editColor"
                  type="text"
                  className="iid-input"
                  placeholder="Midnight Silver"
                  value={color}
                  onChange={e => setColor(e.target.value)}
                  onBlur={() => setTouched(prev => ({ ...prev, color: true }))}
                />
                {touched.color && !isColorValid && (
                  <span className="error-text">Color is required</span>
                )}
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="editMileage">
                  Mileage (Miles) *
                </label>
                <input
                  id="editMileage"
                  type="number"
                  className="iid-input font-mono"
                  value={mileage}
                  onChange={e => setMileage(e.target.value === '' ? '' : Number(e.target.value))}
                  onBlur={() => setTouched(prev => ({ ...prev, mileage: true }))}
                />
                {touched.mileage && !isMileageValid && (
                  <span className="error-text">Non-negative mileage required</span>
                )}
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="editFuel">
                  Fuel Type *
                </label>
                <select
                  id="editFuel"
                  className="iid-input iid-select"
                  value={fuelType}
                  onChange={e => setFuelType(e.target.value as FuelType)}
                >
                  <option value="Petrol">Petrol</option>
                  <option value="Diesel">Diesel</option>
                  <option value="Electric">Electric</option>
                  <option value="Hybrid">Hybrid</option>
                  <option value="PluginHybrid">Plugin Hybrid</option>
                </select>
              </div>
            </div>

            <div className="form-row three-col">
              <div className="form-group">
                <label className="iid-form-label" htmlFor="editPurchase">
                  Purchase Price ($) *
                </label>
                <input
                  id="editPurchase"
                  type="number"
                  className="iid-input font-mono"
                  value={purchasePrice}
                  onChange={e => setPurchasePrice(e.target.value === '' ? '' : Number(e.target.value))}
                  onBlur={() => setTouched(prev => ({ ...prev, purchasePrice: true }))}
                />
                {touched.purchasePrice && !isPurchasePriceValid && (
                  <span className="error-text">Valid purchase price required</span>
                )}
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="editAsking">
                  Asking Price ($) *
                </label>
                <input
                  id="editAsking"
                  type="number"
                  className="iid-input font-mono"
                  value={askingPrice}
                  onChange={e => setAskingPrice(e.target.value === '' ? '' : Number(e.target.value))}
                  onBlur={() => setTouched(prev => ({ ...prev, askingPrice: true }))}
                />
                {touched.askingPrice && !isAskingPriceValid && (
                  <span className="error-text">Valid asking price required</span>
                )}
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="editStatus">
                  Status *
                </label>
                <select
                  id="editStatus"
                  className="iid-input iid-select"
                  value={status}
                  onChange={e => setStatus(e.target.value as VehicleStatus)}
                >
                  <option value="Available">Available</option>
                  <option value="Pending">Pending</option>
                  <option value="Wholesale">Wholesale</option>
                </select>
              </div>
            </div>

            {errorMessage && (
              <div className="bottom-error-alert">
                <span className="material-symbols-outlined">error</span>
                <span>{errorMessage}</span>
              </div>
            )}
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
                <span>Saving Changes...</span>
              ) : (
                <>
                  <span className="material-symbols-outlined">save</span>
                  <span>Save Changes</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
