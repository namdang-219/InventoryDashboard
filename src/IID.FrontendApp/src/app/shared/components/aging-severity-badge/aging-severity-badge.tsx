import clsx from 'clsx';
import { useMemo } from 'react';
import { AgingSeverity } from '../../../core/models/vehicle.model';
import './aging-severity-badge.scss';

export interface AgingSeverityBadgeProps {
  severity?: AgingSeverity | string;
  days?: number | null;
}

export function AgingSeverityBadgeComponent({
  severity = 'None',
  days = null
}: AgingSeverityBadgeProps) {
  const severityClass = useMemo(() => {
    const s = severity?.toLowerCase() ?? 'none';
    return `severity-${s}`;
  }, [severity]);

  const label = useMemo(() => {
    const s = severity ?? 'None';
    return days !== null && days !== undefined ? `${days}d (${s})` : s;
  }, [severity, days]);

  return (
    <span className={clsx('iid-aging-severity-badge', 'aging-badge', severityClass)}>
      {severity === 'Critical' ? (
        <span className="material-symbols-outlined icon">warning</span>
      ) : severity === 'High' ? (
        <span className="material-symbols-outlined icon">schedule</span>
      ) : severity === 'Warning' ? (
        <span className="material-symbols-outlined icon">info</span>
      ) : null}
      <span>{label}</span>
    </span>
  );
}
