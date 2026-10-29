import { FormEvent, useState } from 'react';
import { CreateVehicleRequest, FuelType, VehicleStatus } from '../../../../core/models/vehicle.model';
import { useDealerships } from '../../../../core/services/dealership.service';
import { inventoryService } from '../../../../core/services/inventory.service';
import { useToast } from '../../../../core/services/toast.service';
import { extractErrorMessage } from '../../../../core/utils/error-extractor';
import './create-vehicle-modal.scss';

export interface CreateVehicleModalProps {
  onClose: () => void;
  onCreated?: (id: string) => void;
}

export function CreateVehicleModalComponent({ onClose, onCreated }: CreateVehicleModalProps) {
  const dealershipService = useDealerships();
  const toast = useToast();

  const currentYear = new Date().getFullYear();

  const [dealershipId] = useState<string>(() => dealershipService.selectedDealershipId ?? '');
  const [vin, setVin] = useState<string>('');
  const [vinTouched, setVinTouched] = useState<boolean>(false);
  const [stockNumber, setStockNumber] = useState<string>('');
  const [make, setMake] = useState<string>('');
  const [model, setModel] = useState<string>('');
  const [year, setYear] = useState<number | ''>(currentYear);
  const [color, setColor] = useState<string>('');
  const [mileage, setMileage] = useState<number | ''>(0);
  const [fuelType, setFuelType] = useState<FuelType>('Petrol');
  const [purchasePrice, setPurchasePrice] = useState<number | ''>(25000);
  const [askingPrice, setAskingPrice] = useState<number | ''>(31000);
  const [status, setStatus] = useState<VehicleStatus>('Available');
  const [dateAddedToInventory, setDateAddedToInventory] = useState<string>(() =>
    new Date().toISOString().substring(0, 10)
  );

  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const isVinValid = vin.length >= 11 && vin.length <= 17 && vin.trim().length > 0;
  const isYearValid =
    year !== '' && !Number.isNaN(year) && year >= 1980 && year <= currentYear + 1;
  const isMileageValid = mileage !== '' && !Number.isNaN(mileage) && mileage >= 0;
  const isPurchasePriceValid =
    purchasePrice !== '' && !Number.isNaN(purchasePrice) && purchasePrice >= 1;
  const isAskingPriceValid = askingPrice !== '' && !Number.isNaN(askingPrice) && askingPrice >= 1;

  const isFormValid =
    isVinValid &&
    make.trim().length > 0 &&
    model.trim().length > 0 &&
    isYearValid &&
    color.trim().length > 0 &&
    isMileageValid &&
    Boolean(fuelType) &&
    isPurchasePriceValid &&
    isAskingPriceValid &&
    Boolean(status) &&
    dateAddedToInventory.length > 0;

  function fillSample(preset: 'tesla' | 'bmw' | 'porsche'): void {
    if (preset === 'tesla') {
      setVin('5YJ3E1EB' + Math.floor(100000000 + Math.random() * 900000000));
      setMake('Tesla');
      setModel('Model 3 Performance');
      setYear(2024);
      setColor('Deep Blue Metallic');
      setMileage(4500);
      setFuelType('Electric');
      setPurchasePrice(42000);
      setAskingPrice(49500);
      setStatus('Available');
    } else if (preset === 'bmw') {
      setVin('WBA53AY0' + Math.floor(100000000 + Math.random() * 900000000));
      setMake('BMW');
      setModel('X5 xDrive45e');
      setYear(2023);
      setColor('Mineral White');
      setMileage(18200);
      setFuelType('PluginHybrid');
      setPurchasePrice(51000);
      setAskingPrice(58900);
      setStatus('Available');
    } else {
      setVin('WP0AB2A9' + Math.floor(100000000 + Math.random() * 900000000));
      setMake('Porsche');
      setModel('Taycan 4S');
      setYear(2023);
      setColor('Volcano Grey');
      setMileage(8900);
      setFuelType('Electric');
      setPurchasePrice(85000);
      setAskingPrice(97500);
      setStatus('Available');
    }
  }

  function onSubmit(e: FormEvent<HTMLFormElement>): void {
    e.preventDefault();
    if (!isFormValid) {
      setVinTouched(true);
      return;
    }

    setIsSubmitting(true);
    setErrorMessage(null);

    const dealerId = dealershipService.selectedDealershipId || dealershipId || undefined;
    const req: CreateVehicleRequest = {
      dealershipId: dealerId,
      vin: vin.trim().toUpperCase(),
      stockNumber: stockNumber.trim() || undefined,
      make: make.trim(),
      model: model.trim(),
      year: Number(year),
      color: color.trim(),
      mileage: Number(mileage),
      fuelType,
      purchasePrice: Number(purchasePrice),
      askingPrice: Number(askingPrice),
      status,
      dateAddedToInventory: new Date(dateAddedToInventory).toISOString()
    };

    inventoryService
      .createVehicle(req)
      .then(res => {
        setIsSubmitting(false);
        toast.success('Vehicle Created', `${req.year} ${req.make} ${req.model} added to inventory.`);
        onCreated?.(res.id);
        onClose();
      })
      .catch((err: unknown) => {
        setIsSubmitting(false);
        const msg = extractErrorMessage(err, 'Failed to create vehicle.');
        setErrorMessage(msg);
        toast.error('Failed to Create Vehicle', msg);
      });
  }

  const currentDealer = dealershipService.selectedDealership;

  return (
    <div className="iid-create-vehicle-modal iid-modal-backdrop" onClick={onClose}>
      <div className="modal-card iid-card" onClick={e => e.stopPropagation()}>
        <div className="modal-header">
          <div className="header-title">
            <span className="material-symbols-outlined icon">add_circle</span>
            <h3>Add Vehicle to Inventory</h3>
          </div>
          <button type="button" className="close-btn" onClick={onClose}>
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        <form onSubmit={onSubmit} className="modal-form-wrap">
          <div className="modal-body">
            {/* Quick Sample Preset Fill */}
            <div className="preset-bar">
              <span className="preset-label">Quick Fill:</span>
              <button type="button" className="preset-btn" onClick={() => fillSample('tesla')}>
                Tesla Model 3
              </button>
              <button type="button" className="preset-btn" onClick={() => fillSample('bmw')}>
                BMW X5
              </button>
              <button type="button" className="preset-btn" onClick={() => fillSample('porsche')}>
                Porsche Taycan
              </button>
            </div>

            {errorMessage && (
              <div className="error-banner">
                <span className="material-symbols-outlined">error</span>
                <span>{errorMessage}</span>
              </div>
            )}

            <div className="form-group" style={{ marginBottom: '14px' }}>
              <label className="iid-form-label">Dealership Showroom</label>
              <div className="dealership-display-badge">
                <span className="material-symbols-outlined store-icon">storefront</span>
                <span className="dealer-name">{currentDealer?.name || 'All Dealerships'}</span>
                {currentDealer && (
                  <span className="dealer-location">
                    — {currentDealer.city}, {currentDealer.state}
                  </span>
                )}
              </div>
            </div>

            <div className="form-row two-col">
              <div className="form-group">
                <label className="iid-form-label" htmlFor="vehVin">
                  VIN (17 Chars) *
                </label>
                <input
                  id="vehVin"
                  type="text"
                  className="iid-input font-mono"
                  placeholder="e.g. 5YJ3E1EB..."
                  value={vin}
                  onChange={e => setVin(e.target.value)}
                  onBlur={() => setVinTouched(true)}
                />
                {vinTouched && !isVinValid && (
                  <span className="error-text">VIN must be between 11 and 17 characters</span>
                )}
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="vehStock">
                  Stock Number (Optional)
                </label>
                <input
                  id="vehStock"
                  type="text"
                  className="iid-input font-mono"
                  placeholder="Auto-generated if empty"
                  value={stockNumber}
                  onChange={e => setStockNumber(e.target.value)}
                />
              </div>
            </div>

            <div className="form-row three-col">
              <div className="form-group">
                <label className="iid-form-label" htmlFor="vehMake">
                  Make *
                </label>
                <input
                  id="vehMake"
                  type="text"
                  className="iid-input"
                  placeholder="Tesla"
                  value={make}
                  onChange={e => setMake(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="vehModel">
                  Model *
                </label>
                <input
                  id="vehModel"
                  type="text"
                  className="iid-input"
                  placeholder="Model 3"
                  value={model}
                  onChange={e => setModel(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="vehYear">
                  Year *
                </label>
                <input
                  id="vehYear"
                  type="number"
                  className="iid-input font-mono"
                  value={year}
                  onChange={e => setYear(e.target.value === '' ? '' : Number(e.target.value))}
                />
              </div>
            </div>

            <div className="form-row three-col">
              <div className="form-group">
                <label className="iid-form-label" htmlFor="vehColor">
                  Color *
                </label>
                <input
                  id="vehColor"
                  type="text"
                  className="iid-input"
                  placeholder="Midnight Silver"
                  value={color}
                  onChange={e => setColor(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="vehMileage">
                  Mileage (Miles) *
                </label>
                <input
                  id="vehMileage"
                  type="number"
                  className="iid-input font-mono"
                  value={mileage}
                  onChange={e => setMileage(e.target.value === '' ? '' : Number(e.target.value))}
                />
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="vehFuel">
                  Fuel Type *
                </label>
                <select
                  id="vehFuel"
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
                <label className="iid-form-label" htmlFor="vehPurchase">
                  Purchase Price ($) *
                </label>
                <input
                  id="vehPurchase"
                  type="number"
                  className="iid-input font-mono"
                  value={purchasePrice}
                  onChange={e => setPurchasePrice(e.target.value === '' ? '' : Number(e.target.value))}
                />
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="vehAsking">
                  Asking Price ($) *
                </label>
                <input
                  id="vehAsking"
                  type="number"
                  className="iid-input font-mono"
                  value={askingPrice}
                  onChange={e => setAskingPrice(e.target.value === '' ? '' : Number(e.target.value))}
                />
              </div>

              <div className="form-group">
                <label className="iid-form-label" htmlFor="vehStatus">
                  Status *
                </label>
                <select
                  id="vehStatus"
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

            <div className="form-row one-col">
              <div className="form-group">
                <label className="iid-form-label" htmlFor="vehDateAdded">
                  Date Added To Inventory *
                </label>
                <input
                  id="vehDateAdded"
                  type="date"
                  className="iid-input font-mono"
                  value={dateAddedToInventory}
                  onChange={e => setDateAddedToInventory(e.target.value)}
                />
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
                <span>Saving Vehicle...</span>
              ) : (
                <>
                  <span className="material-symbols-outlined">save</span>
                  <span>Create Vehicle</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
