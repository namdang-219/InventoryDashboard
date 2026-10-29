import clsx from 'clsx';
import { useMemo } from 'react';
import { DemandLevel } from '../../../core/models/vehicle.model';
import './demand-badge.scss';

export interface DemandBadgeProps {
  level?: DemandLevel | string;
  score?: number | null;
}

export function DemandBadgeComponent({
  level = 'Medium',
  score = null
}: DemandBadgeProps) {
  const levelClass = useMemo(() => {
    const l = level?.toLowerCase() ?? 'medium';
    return `demand-${l}`;
  }, [level]);

  const icon = useMemo(() => {
    const l = level?.toLowerCase() ?? 'medium';
    switch (l) {
      case 'high':
        return 'trending_up';
      case 'low':
        return 'trending_down';
      default:
        return 'trending_flat';
    }
  }, [level]);

  return (
    <span className={clsx('iid-demand-badge', 'demand-badge', levelClass)}>
      <span className="material-symbols-outlined icon">{icon}</span>
      <span className="level-text">{level} Demand</span>
      {score !== null && score !== undefined && <span className="score-pill">{score}</span>}
    </span>
  );
}
