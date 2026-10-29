import clsx from 'clsx';
import './stat-card.scss';

export interface StatCardProps {
  title: string;
  value: string | number;
  icon?: string;
  accent?: 'primary' | 'success' | 'warning' | 'danger' | 'purple';
  trend?: 'up' | 'down' | 'flat' | null;
  trendLabel?: string | null;
  subtitle?: string | null;
}

export function StatCardComponent({
  title,
  value,
  icon = 'analytics',
  accent = 'primary',
  trend = null,
  trendLabel = null,
  subtitle = null
}: StatCardProps) {
  return (
    <div className={clsx('iid-stat-card', 'stat-card', 'iid-card', `accent-${accent}`)}>
      <div className="card-glow" />
      <div className="card-top">
        <span className="card-title">{title}</span>
        <div className="icon-wrap">
          <span className="material-symbols-outlined">{icon}</span>
        </div>
      </div>

      <div className="card-middle">
        <span className="card-value">{value}</span>
      </div>

      {(trend || subtitle || trendLabel) && (
        <div className="card-bottom">
          {trend && (
            <span className={clsx('trend-badge', `trend-${trend}`)}>
              {trend === 'up' ? (
                <span className="material-symbols-outlined trend-icon">arrow_upward</span>
              ) : trend === 'down' ? (
                <span className="material-symbols-outlined trend-icon">arrow_downward</span>
              ) : (
                <span className="material-symbols-outlined trend-icon">horizontal_rule</span>
              )}
              <span>{trendLabel || trend}</span>
            </span>
          )}
          {subtitle && <span className="card-subtitle">{subtitle}</span>}
        </div>
      )}
    </div>
  );
}
