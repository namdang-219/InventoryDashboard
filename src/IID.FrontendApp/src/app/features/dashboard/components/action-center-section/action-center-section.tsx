import clsx from 'clsx';
import { useMemo, useState } from 'react';
import { ActionCenterItem } from '../../../../core/models/dashboard.model';
import './action-center-section.scss';

export type ActionFilter = 'all' | 'critical' | 'warning' | 'aging' | 'demand';
export type ActionSort = 'priority' | 'demand-asc' | 'demand-desc' | 'aging-desc';

export interface ActionCenterSectionProps {
  items?: readonly ActionCenterItem[];
  onActionClick?: (item: ActionCenterItem) => void;
}

function extractDemandScore(item: ActionCenterItem): number | null {
  const match = item.description.match(/demand score (\d+)/i);
  return match ? parseInt(match[1], 10) : null;
}

function extractDaysOnLot(item: ActionCenterItem): number | null {
  const match = item.description.match(/(\d+) days on lot/i);
  return match ? parseInt(match[1], 10) : null;
}

function getDemandScore(item: ActionCenterItem): number | null {
  if (item.demandScore !== undefined && item.demandScore !== null) {
    return item.demandScore;
  }
  return extractDemandScore(item);
}

function getDaysOnLot(item: ActionCenterItem): number | null {
  if (item.daysOnLot !== undefined && item.daysOnLot !== null) {
    return item.daysOnLot;
  }
  return extractDaysOnLot(item);
}

export function ActionCenterSectionComponent({ items = [], onActionClick }: ActionCenterSectionProps) {
  const [activeFilter, setActiveFilter] = useState<ActionFilter>('all');
  const [activeSort, setActiveSort] = useState<ActionSort>('priority');

  const criticalCount = useMemo(() => items.filter(i => i.severity === 'critical').length, [items]);
  const warningCount = useMemo(() => items.filter(i => i.severity === 'warning').length, [items]);
  const demandCount = useMemo(() => items.filter(i => i.category === 'demand').length, [items]);

  const filteredAndSortedItems = useMemo(() => {
    let list = [...items];

    // 1. Filter
    if (activeFilter === 'critical') {
      list = list.filter(i => i.severity === 'critical');
    } else if (activeFilter === 'warning') {
      list = list.filter(i => i.severity === 'warning');
    } else if (activeFilter === 'aging') {
      list = list.filter(i => i.category === 'aging');
    } else if (activeFilter === 'demand') {
      list = list.filter(i => i.category === 'demand');
    }

    // 2. Sort
    if (activeSort === 'priority') {
      // Priority: Critical (0) -> Warning (1) -> Info (2), then lowest Demand Score first
      const sevOrder: Record<string, number> = { critical: 0, warning: 1, info: 2 };
      list.sort((a, b) => {
        const sevA = sevOrder[a.severity] ?? 3;
        const sevB = sevOrder[b.severity] ?? 3;
        if (sevA !== sevB) return sevA - sevB;

        const scoreA = a.demandScore ?? extractDemandScore(a) ?? 100;
        const scoreB = b.demandScore ?? extractDemandScore(b) ?? 100;
        if (scoreA !== scoreB) return scoreA - scoreB;

        const daysA = a.daysOnLot ?? extractDaysOnLot(a) ?? 0;
        const daysB = b.daysOnLot ?? extractDaysOnLot(b) ?? 0;
        return daysB - daysA;
      });
    } else if (activeSort === 'demand-asc') {
      list.sort((a, b) => {
        const scoreA = a.demandScore ?? extractDemandScore(a) ?? 100;
        const scoreB = b.demandScore ?? extractDemandScore(b) ?? 100;
        return scoreA - scoreB;
      });
    } else if (activeSort === 'demand-desc') {
      list.sort((a, b) => {
        const scoreA = a.demandScore ?? extractDemandScore(a) ?? 0;
        const scoreB = b.demandScore ?? extractDemandScore(b) ?? 0;
        return scoreB - scoreA;
      });
    } else if (activeSort === 'aging-desc') {
      list.sort((a, b) => {
        const daysA = a.daysOnLot ?? extractDaysOnLot(a) ?? 0;
        const daysB = b.daysOnLot ?? extractDaysOnLot(b) ?? 0;
        return daysB - daysA;
      });
    }

    return list;
  }, [items, activeFilter, activeSort]);

  const handleItemClick = (item: ActionCenterItem) => {
    onActionClick?.(item);
  };

  return (
    <div className="iid-action-center-section action-center iid-card">
      {/* Header */}
      <div className="action-header">
        <div className="header-left">
          <div className="header-title">
            <span className="material-symbols-outlined icon">bolt</span>
            <h3>Action Center &amp; Smart Recommendations</h3>
          </div>
          <span className="badge">{items.length} Attention Required</span>
        </div>

        {/* Toolbar Filters & Sort */}
        <div className="header-controls">
          {/* Filter Tabs */}
          <div className="filter-tabs">
            <button
              type="button"
              className={clsx('tab-btn', activeFilter === 'all' && 'active')}
              onClick={() => setActiveFilter('all')}
            >
              All ({items.length})
            </button>
            {criticalCount > 0 && (
              <button
                type="button"
                className={clsx('tab-btn', 'critical', activeFilter === 'critical' && 'active')}
                onClick={() => setActiveFilter('critical')}
              >
                <span className="dot" />
                Critical ({criticalCount})
              </button>
            )}
            {warningCount > 0 && (
              <button
                type="button"
                className={clsx('tab-btn', 'warning', activeFilter === 'warning' && 'active')}
                onClick={() => setActiveFilter('warning')}
              >
                <span className="dot" />
                Warning ({warningCount})
              </button>
            )}
            {demandCount > 0 && (
              <button
                type="button"
                className={clsx('tab-btn', 'demand', activeFilter === 'demand' && 'active')}
                onClick={() => setActiveFilter('demand')}
              >
                Demand ({demandCount})
              </button>
            )}
          </div>

          {/* Sort Dropdown */}
          <div className="sort-wrap">
            <span className="material-symbols-outlined sort-icon">sort</span>
            <span className="sort-label">Sort:</span>
            <select
              className="sort-select"
              value={activeSort}
              onChange={e => setActiveSort(e.target.value as ActionSort)}
            >
              <option value="priority">Priority (Critical &amp; Low Demand)</option>
              <option value="demand-asc">Demand Score (Lowest First)</option>
              <option value="demand-desc">Demand Score (Highest First)</option>
              <option value="aging-desc">Aging Days (Highest First)</option>
            </select>
          </div>
        </div>
      </div>

      {/* Content: Compact List */}
      {filteredAndSortedItems.length === 0 ? (
        <div className="empty-actions">
          <span className="material-symbols-outlined">verified</span>
          <p>All stock parameters within optimal velocity! No adjustments required in this view.</p>
        </div>
      ) : (
        <div className="compact-list-container">
          <div className="compact-action-list">
            {filteredAndSortedItems.map((item, idx) => {
              const score = getDemandScore(item);
              const days = getDaysOnLot(item);

              return (
                <div
                  key={item.id}
                  className={clsx('compact-action-row', `sev-${item.severity}`)}
                  onClick={() => handleItemClick(item)}
                >
                  {/* Severity Indicator & Icon */}
                  <div className="row-indicator">
                    <span className="rank-index">#{idx + 1}</span>
                    <div className="row-icon">
                      {item.severity === 'critical' ? (
                        <span className="material-symbols-outlined icon-crit">crisis_alert</span>
                      ) : item.severity === 'warning' ? (
                        <span className="material-symbols-outlined icon-warn">warning</span>
                      ) : (
                        <span className="material-symbols-outlined icon-info">trending_down</span>
                      )}
                    </div>
                  </div>

                  {/* Main Info */}
                  <div className="row-content">
                    <div className="row-top">
                      <span className={clsx('category-badge', `cat-${item.category}`)}>{item.category}</span>
                      <span className={clsx('severity-pill', `sev-pill-${item.severity}`)}>{item.severity}</span>
                      <h4 className="row-title">{item.title}</h4>
                    </div>
                    <p className="row-desc">{item.description}</p>
                  </div>

                  {/* Metrics Pills */}
                  <div className="row-metrics">
                    {score ? (
                      <div
                        className={clsx(
                          'metric-pill',
                          'demand',
                          score < 40 && 'low',
                          score >= 40 && score < 70 && 'med',
                          score >= 70 && 'high'
                        )}
                        title="Demand Score"
                      >
                        <span className="material-symbols-outlined">speed</span>
                        <span className="metric-val">{score}/100</span>
                        <span className="metric-lbl">Demand</span>
                      </div>
                    ) : null}

                    {days ? (
                      <div
                        className={clsx(
                          'metric-pill',
                          'aging',
                          days >= 90 && 'crit',
                          days >= 60 && days < 90 && 'warn'
                        )}
                        title="Days on lot"
                      >
                        <span className="material-symbols-outlined">schedule</span>
                        <span className="metric-val">{days}d</span>
                        <span className="metric-lbl">on lot</span>
                      </div>
                    ) : null}
                  </div>

                  {/* Action CTA */}
                  <div className="row-cta">
                    <button
                      type="button"
                      className="iid-btn iid-btn-secondary iid-btn-sm action-btn"
                      onClick={e => {
                        e.stopPropagation();
                        handleItemClick(item);
                      }}
                    >
                      <span>Take Action</span>
                      <span className="material-symbols-outlined">arrow_forward</span>
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
