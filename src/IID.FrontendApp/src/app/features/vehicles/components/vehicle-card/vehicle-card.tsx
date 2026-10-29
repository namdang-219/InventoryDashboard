import { Vehicle } from '../../../../core/models/vehicle.model';
import { AgingSeverityBadgeComponent } from '../../../../shared/components/aging-severity-badge/aging-severity-badge';
import { DemandBadgeComponent } from '../../../../shared/components/demand-badge/demand-badge';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { formatCurrency } from '../../../../shared/pipes/format-currency.pipe';
import './vehicle-card.scss';

export interface VehicleCardProps {
  vehicle: Vehicle;
  isManager?: boolean;
  canMarkSold?: boolean;
  onViewDetails?: (v: Vehicle) => void;
  onEditVehicle?: (v: Vehicle) => void;
  onMarkSold?: (v: Vehicle) => void;
  onLogAction?: (v: Vehicle) => void;
  onTransferDealership?: (v: Vehicle) => void;
}

export function VehicleCardComponent({
  vehicle,
  isManager = false,
  canMarkSold = false,
  onViewDetails,
  onEditVehicle,
  onMarkSold,
  onLogAction,
  onTransferDealership
}: VehicleCardProps) {
  return (
    <div
      className="iid-vehicle-card card-wrap iid-card iid-card-hover"
      onClick={() => onViewDetails?.(vehicle)}
    >
      {/* Card Header */}
      <div className="card-top">
        <div className="badge-cluster">
          <StatusBadgeComponent status={vehicle.status} />
          <span className="fuel-badge">{vehicle.fuelType}</span>
        </div>
        <div className={`days-indicator${vehicle.isAging ? ' aging-alert' : ''}`}>
          <span className="material-symbols-outlined">schedule</span>
          <span>{vehicle.daysInInventory}d</span>
        </div>
      </div>

      {/* Vehicle Title */}
      <div className="card-title-section">
        <h3 className="title">
          {vehicle.year} {vehicle.make} {vehicle.model}
        </h3>
        <div className="vin-stock">
          <span>VIN: {vehicle.vin}</span>
          <span className="stock-pill">{vehicle.stockNumber}</span>
        </div>
      </div>

      {/* Metadata Grid */}
      <div className="meta-row">
        <div className="meta-item">
          <span className="meta-label">Mileage</span>
          <span className="meta-val">{vehicle.mileage.toLocaleString()} mi</span>
        </div>
        <div className="meta-item">
          <span className="meta-label">Color</span>
          <span className="meta-val">{vehicle.color}</span>
        </div>
        <div className="meta-item">
          <span className="meta-label">Inflow Date</span>
          <span className="meta-val">{vehicle.dateAddedToInventoryUtc.substring(0, 10)}</span>
        </div>
      </div>

      {/* Analytics Row */}
      <div className="analytics-row">
        <AgingSeverityBadgeComponent
          severity={vehicle.agingSeverity}
          days={vehicle.daysInInventory}
        />
        <DemandBadgeComponent level={vehicle.demandLevel} score={vehicle.demandScore} />
      </div>

      {/* Price & Actions Footer */}
      <div className="card-footer">
        <div className="price-box">
          <span className="price-label">Asking Price</span>
          <span className="price-val">{formatCurrency(vehicle.askingPriceAmount)}</span>
        </div>

        <div className="actions-group" onClick={e => e.stopPropagation()}>
          <button
            type="button"
            className="iid-btn-icon"
            title="View Details"
            onClick={() => onViewDetails?.(vehicle)}
          >
            <span className="material-symbols-outlined">visibility</span>
          </button>

          {isManager && vehicle.status !== 'Sold' && (
            <>
              <button
                type="button"
                className="iid-btn-icon"
                title="Edit Vehicle"
                onClick={() => onEditVehicle?.(vehicle)}
              >
                <span className="material-symbols-outlined">edit</span>
              </button>

              <button
                type="button"
                className="iid-btn-icon"
                title="Log Action"
                onClick={() => onLogAction?.(vehicle)}
              >
                <span className="material-symbols-outlined">assignment_add</span>
              </button>

              <button
                type="button"
                className="iid-btn-icon"
                title="Transfer Dealership Showroom"
                onClick={() => onTransferDealership?.(vehicle)}
              >
                <span className="material-symbols-outlined">swap_horiz</span>
              </button>
            </>
          )}

          {canMarkSold && vehicle.status !== 'Sold' && (
            <button
              type="button"
              className="iid-btn iid-btn-primary iid-btn-sm"
              title="Mark Vehicle Sold"
              onClick={() => onMarkSold?.(vehicle)}
            >
              <span className="material-symbols-outlined">monetization_on</span>
              <span>Sold</span>
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
