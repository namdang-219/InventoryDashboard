import clsx from 'clsx';
import { useMemo } from 'react';
import { VehicleStatus } from '../../../core/models/vehicle.model';
import './status-badge.scss';

export interface StatusBadgeProps {
  status?: VehicleStatus | string;
}

export function StatusBadgeComponent({ status = 'Available' }: StatusBadgeProps) {
  const badgeClass = useMemo(() => {
    const s = status?.toLowerCase() ?? '';
    switch (s) {
      case 'available':
        return 'status-available';
      case 'sold':
        return 'status-sold';
      case 'pending':
        return 'status-pending';
      case 'wholesale':
        return 'status-wholesale';
      default:
        return 'status-default';
    }
  }, [status]);

  const icon = useMemo(() => {
    const s = status?.toLowerCase() ?? '';
    switch (s) {
      case 'available':
        return 'check_circle';
      case 'sold':
        return 'monetization_on';
      case 'pending':
        return 'hourglass_top';
      case 'wholesale':
        return 'local_shipping';
      default:
        return 'help';
    }
  }, [status]);

  return (
    <span className={clsx('iid-status-badge', 'status-badge', badgeClass)}>
      <span className="status-dot" />
      <span className="material-symbols-outlined status-icon">{icon}</span>
      <span className="status-text">{status}</span>
    </span>
  );
}
