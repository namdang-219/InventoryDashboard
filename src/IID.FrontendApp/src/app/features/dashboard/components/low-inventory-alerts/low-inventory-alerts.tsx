import { LowInventoryAlert } from '../../../../core/models/dashboard.model';
import './low-inventory-alerts.scss';

export interface LowInventoryAlertsProps {
  alerts?: readonly LowInventoryAlert[];
}

export function LowInventoryAlertsComponent({ alerts = [] }: LowInventoryAlertsProps) {
  return (
    <div className="iid-low-inventory-alerts low-inventory-card iid-card">
      <div className="card-header">
        <div className="header-left">
          <span className="material-symbols-outlined icon">crisis_alert</span>
          <h4>Low Stock Model Alerts</h4>
        </div>
        <span className="threshold-tag">Threshold &le; Min</span>
      </div>

      {alerts.length === 0 ? (
        <div className="empty-state">
          <span className="material-symbols-outlined">check_circle</span>
          <p>All model supplies are healthy across all segments.</p>
        </div>
      ) : (
        <div className="alerts-table-wrap">
          <table className="alerts-table">
            <thead>
              <tr>
                <th>Make</th>
                <th>Model</th>
                <th>Available</th>
                <th>Min Threshold</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {alerts.map(alert => (
                <tr key={alert.id}>
                  <td className="font-bold">{alert.make}</td>
                  <td>{alert.model}</td>
                  <td>
                    <span className="count-critical">{alert.available}</span>
                  </td>
                  <td>{alert.threshold} units</td>
                  <td>
                    <span className="deficit-pill">
                      Low ({alert.threshold - alert.available} deficit)
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
