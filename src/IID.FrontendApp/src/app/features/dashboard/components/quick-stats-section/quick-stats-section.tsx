import { useMemo } from 'react';
import { DashboardSummary, QuickStats } from '../../../../core/models/dashboard.model';
import { useAuth } from '../../../../core/services/auth.service';
import { StatCardComponent } from '../../../../shared/components/stat-card/stat-card';
import './quick-stats-section.scss';

export interface QuickStatsSectionProps {
  summary?: DashboardSummary | null;
  quickStats?: QuickStats | null;
  isManager?: boolean;
}

export function QuickStatsSectionComponent({
  summary = null,
  quickStats: _quickStats = null,
  isManager
}: QuickStatsSectionProps) {
  const auth = useAuth();
  const resolvedIsManager = isManager ?? auth.isManager;

  const totalValueFormatted = useMemo(() => {
    return summary
      ? new Intl.NumberFormat('en-US', {
          style: 'currency',
          currency: 'USD',
          maximumFractionDigits: 0
        }).format(summary.totalInventoryValue)
      : '$0';
  }, [summary]);

  const avgValueFormatted = useMemo(() => {
    return summary
      ? new Intl.NumberFormat('en-US', {
          style: 'currency',
          currency: 'USD',
          maximumFractionDigits: 0
        }).format(summary.averageAskingPrice)
      : '$0';
  }, [summary]);

  const agingCount = summary?.agingCount ?? 0;

  return (
    <div className="iid-quick-stats-section stats-grid">
      {/* Card 1: Total Units */}
      <StatCardComponent
        title="Total Inventory"
        value={summary?.totalInventory ?? 0}
        icon="inventory_2"
        accent="primary"
        subtitle="All active & pending stock"
      />

      {/* Card 2: Available Units */}
      <StatCardComponent
        title="Ready for Sale"
        value={summary?.availableCount ?? 0}
        icon="check_circle"
        accent="success"
        trend="up"
        trendLabel="Available"
      />

      {/* Card 3: Aging Warning (>90 Days) */}
      <StatCardComponent
        title="Aging Stock (>90d)"
        value={agingCount}
        icon="warning"
        accent={agingCount > 0 ? 'danger' : 'success'}
        trend={agingCount > 0 ? 'down' : 'flat'}
        trendLabel={agingCount > 0 ? 'Needs Action' : 'Optimal'}
      />

      {/* Card 4: Avg Days on Lot */}
      <StatCardComponent
        title="Avg Days on Lot"
        value={`${summary?.averageDaysOnLot ?? 0} days`}
        icon="schedule"
        accent="warning"
        subtitle="Lot turnaround velocity"
      />

      {resolvedIsManager && (
        <>
          {/* Card 5: Total Capital Value */}
          <StatCardComponent
            title="Total Portfolio Value"
            value={totalValueFormatted}
            icon="account_balance_wallet"
            accent="purple"
            subtitle="Total asking price"
          />

          {/* Card 6: Average Unit Value */}
          <StatCardComponent
            title="Avg Unit Valuation"
            value={avgValueFormatted}
            icon="payments"
            accent="primary"
            subtitle="Mean asking price"
          />
        </>
      )}
    </div>
  );
}
