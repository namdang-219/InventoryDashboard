import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { ActionCenterItem } from '../../../../core/models/dashboard.model';

export type ActionFilter = 'all' | 'critical' | 'warning' | 'aging' | 'demand';
export type ActionSort = 'priority' | 'demand-asc' | 'demand-desc' | 'aging-desc';

@Component({
  selector: 'iid-action-center-section',
  standalone: true,
  templateUrl: './action-center-section.component.html',
  styleUrl: './action-center-section.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ActionCenterSectionComponent {
  readonly items = input<readonly ActionCenterItem[]>([]);
  readonly actionClick = output<ActionCenterItem>();

  readonly activeFilter = signal<ActionFilter>('all');
  readonly activeSort = signal<ActionSort>('priority');

  readonly criticalCount = computed(() => this.items().filter(i => i.severity === 'critical').length);
  readonly warningCount = computed(() => this.items().filter(i => i.severity === 'warning').length);
  readonly agingCount = computed(() => this.items().filter(i => i.category === 'aging').length);
  readonly demandCount = computed(() => this.items().filter(i => i.category === 'demand').length);

  readonly filteredAndSortedItems = computed(() => {
    let list = [...this.items()];

    // 1. Filter
    const filter = this.activeFilter();
    if (filter === 'critical') {
      list = list.filter(i => i.severity === 'critical');
    } else if (filter === 'warning') {
      list = list.filter(i => i.severity === 'warning');
    } else if (filter === 'aging') {
      list = list.filter(i => i.category === 'aging');
    } else if (filter === 'demand') {
      list = list.filter(i => i.category === 'demand');
    }

    // 2. Sort
    const sort = this.activeSort();
    if (sort === 'priority') {
      // Priority: Critical (0) -> Warning (1) -> Info (2), then lowest Demand Score first
      list.sort((a, b) => {
        const sevOrder: Record<string, number> = { critical: 0, warning: 1, info: 2 };
        const sevA = sevOrder[a.severity] ?? 3;
        const sevB = sevOrder[b.severity] ?? 3;
        if (sevA !== sevB) return sevA - sevB;

        const scoreA = a.demandScore ?? this.extractDemandScore(a) ?? 100;
        const scoreB = b.demandScore ?? this.extractDemandScore(b) ?? 100;
        if (scoreA !== scoreB) return scoreA - scoreB;

        const daysA = a.daysOnLot ?? this.extractDaysOnLot(a) ?? 0;
        const daysB = b.daysOnLot ?? this.extractDaysOnLot(b) ?? 0;
        return daysB - daysA;
      });
    } else if (sort === 'demand-asc') {
      list.sort((a, b) => {
        const scoreA = a.demandScore ?? this.extractDemandScore(a) ?? 100;
        const scoreB = b.demandScore ?? this.extractDemandScore(b) ?? 100;
        return scoreA - scoreB;
      });
    } else if (sort === 'demand-desc') {
      list.sort((a, b) => {
        const scoreA = a.demandScore ?? this.extractDemandScore(a) ?? 0;
        const scoreB = b.demandScore ?? this.extractDemandScore(b) ?? 0;
        return scoreB - scoreA;
      });
    } else if (sort === 'aging-desc') {
      list.sort((a, b) => {
        const daysA = a.daysOnLot ?? this.extractDaysOnLot(a) ?? 0;
        const daysB = b.daysOnLot ?? this.extractDaysOnLot(b) ?? 0;
        return daysB - daysA;
      });
    }

    return list;
  });

  getDemandScore(item: ActionCenterItem): number | null {
    if (item.demandScore !== undefined && item.demandScore !== null) {
      return item.demandScore;
    }
    return this.extractDemandScore(item);
  }

  getDaysOnLot(item: ActionCenterItem): number | null {
    if (item.daysOnLot !== undefined && item.daysOnLot !== null) {
      return item.daysOnLot;
    }
    return this.extractDaysOnLot(item);
  }

  private extractDemandScore(item: ActionCenterItem): number | null {
    const match = item.description.match(/demand score (\d+)/i);
    return match ? parseInt(match[1], 10) : null;
  }

  private extractDaysOnLot(item: ActionCenterItem): number | null {
    const match = item.description.match(/(\d+) days on lot/i);
    return match ? parseInt(match[1], 10) : null;
  }

  onItemClick(item: ActionCenterItem): void {
    this.actionClick.emit(item);
  }
}
