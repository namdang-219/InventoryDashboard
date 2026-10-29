import { Vehicle } from '../../../../core/models/vehicle.model';
import { AgingSeverityBadgeComponent } from '../../../../shared/components/aging-severity-badge/aging-severity-badge';
import { DemandBadgeComponent } from '../../../../shared/components/demand-badge/demand-badge';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { formatCurrency } from '../../../../shared/pipes/format-currency.pipe';
import './vehicle-table.scss';

export interface VehicleTableProps {
  vehicles: readonly Vehicle[];
  isManager?: boolean;
  canMarkSold?: boolean;
  onViewDetails?: (v: Vehicle) => void;
  onEditVehicle?: (v: Vehicle) => void;
  onMarkSold?: (v: Vehicle) => void;
  onLogAction?: (v: Vehicle) => void;
  onTransferDealership?: (v: Vehicle) => void;
}

export function VehicleTableComponent({
  vehicles,
  isManager = false,
  canMarkSold = false,
  onViewDetails,
  onEditVehicle,
  onMarkSold,
  onLogAction,
  onTransferDealership
}: VehicleTableProps) {
  return (
    <div className="iid-vehicle-table table-card iid-card">
      <div className="table-wrap">
        <table className="data-table">
          <thead>
            <tr>
              <th>Vehicle</th>
              <th>VIN / Stock</th>
              <th>Fuel</th>
              <th>Mileage</th>
              <th>Asking Price</th>
              <th>Status</th>
              <th>Lot Days</th>
              <th>Aging State</th>
              <th>Demand</th>
              <th className="col-actions">Actions</th>
            </tr>
          </thead>
          <tbody>
            {vehicles.map(veh => (
              <tr key={veh.id} className="data-row" onClick={() => onViewDetails?.(veh)}>
                <td className="col-vehicle">
                  <div className="veh-title">
                    {veh.year} {veh.make} {veh.model}
                  </div>
                  <div className="veh-color">{veh.color}</div>
                </td>
                <td className="col-vin">
                  <div className="vin-mono">{veh.vin}</div>
                  <div className="stock-mono">{veh.stockNumber}</div>
                </td>
                <td>
                  <span className="fuel-chip">{veh.fuelType}</span>
                </td>
                <td className="col-mileage">{veh.mileage.toLocaleString()} mi</td>
                <td className="col-price">{formatCurrency(veh.askingPriceAmount)}</td>
                <td>
                  <StatusBadgeComponent status={veh.status} />
                </td>
                <td>
                  <span className={`days-pill${veh.isAging ? ' aging-danger' : ''}`}>
                    {veh.daysInInventory}d
                  </span>
                </td>
                <td>
                  <AgingSeverityBadgeComponent severity={veh.agingSeverity} />
                </td>
                <td>
                  <DemandBadgeComponent level={veh.demandLevel} score={veh.demandScore} />
                </td>
                <td className="col-actions" onClick={e => e.stopPropagation()}>
                  <div className="btn-group">
                    <button
                      type="button"
                      className="iid-btn-icon"
                      title="View Details"
                      onClick={() => onViewDetails?.(veh)}
                    >
                      <span className="material-symbols-outlined">visibility</span>
                    </button>

                    {isManager && veh.status !== 'Sold' && (
                      <>
                        <button
                          type="button"
                          className="iid-btn-icon"
                          title="Edit Vehicle"
                          onClick={() => onEditVehicle?.(veh)}
                        >
                          <span className="material-symbols-outlined">edit</span>
                        </button>

                        <button
                          type="button"
                          className="iid-btn-icon"
                          title="Log Action"
                          onClick={() => onLogAction?.(veh)}
                        >
                          <span className="material-symbols-outlined">assignment_add</span>
                        </button>

                        <button
                          type="button"
                          className="iid-btn-icon"
                          title="Transfer Dealership Showroom"
                          onClick={() => onTransferDealership?.(veh)}
                        >
                          <span className="material-symbols-outlined">swap_horiz</span>
                        </button>
                      </>
                    )}

                    {canMarkSold && veh.status !== 'Sold' && (
                      <button
                        type="button"
                        className="iid-btn iid-btn-primary iid-btn-sm"
                        title="Mark Sold"
                        onClick={() => onMarkSold?.(veh)}
                      >
                        <span>Sold</span>
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
