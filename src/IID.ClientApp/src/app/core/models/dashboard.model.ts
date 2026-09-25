import { AgingSeverity, DemandLevel, FuelType, VehicleStatus } from './vehicle.model';

export interface DashboardBundle {
  summary: DashboardSummary;
  quickStats: QuickStats;
  charts: InventoryCharts;
  actionCenter: readonly ActionCenterItem[];
  aiInsights: readonly AiInsight[];
  inventory: DashboardInventory;
}

export interface DashboardSummary {
  generatedAtUtc: string;
  totalInventory: number;
  availableCount: number;
  pendingCount: number;
  soldCount: number;
  wholesaleCount: number;
  agingCount: number;
  agingBySeverity: Record<string, number>;
  totalInventoryValue: number;
  averageAskingPrice: number;
  averageDaysOnLot: number;
  topMakes: readonly MakeDistribution[];
  fuelMix: readonly FuelTypeDistribution[];
  demandDistribution: readonly DemandLevelDistribution[];
}

export interface MakeDistribution {
  make: string;
  count: number;
  totalValue: number;
}

export interface FuelTypeDistribution {
  fuelType: string;
  count: number;
}

export interface DemandLevelDistribution {
  demandLevel: string;
  count: number;
}

export interface QuickStats {
  items: readonly QuickStatItem[];
}

export interface QuickStatItem {
  label: string;
  value: string;
  trend?: 'up' | 'down' | 'flat' | null;
  trendLabel?: string | null;
  icon: string;
  accent: 'primary' | 'success' | 'warning' | 'danger';
}

export interface InventoryCharts {
  statusBreakdown: readonly StatusBreakdown[];
  fuelBreakdown: readonly FuelBreakdown[];
  agingHistogram: readonly AgingBucket[];
  monthlySales: readonly MonthlySales[];
}

export interface StatusBreakdown {
  status: string;
  count: number;
}

export interface FuelBreakdown {
  fuelType: string;
  count: number;
}

export interface AgingBucket {
  bucket: string;
  count: number;
}

export interface MonthlySales {
  month: string;
  sold: number;
  revenue: number;
}

export interface ActionCenterItem {
  id: string;
  category: 'pricing' | 'aging' | 'demand' | 'follow-up' | string;
  title: string;
  description: string;
  vehicleId?: string | null;
  severity: 'info' | 'warning' | 'critical';
  demandScore?: number | null;
  daysOnLot?: number | null;
}

export interface AiInsight {
  id: string;
  title: string;
  body: string;
  confidence: number;
  vehicleId?: string | null;
  createdAt: string;
}

export interface DashboardInventory {
  items: readonly DashboardVehicle[];
  page: number;
  limit: number;
  total: number;
}

export interface DashboardVehicle {
  id: string;
  vin: string;
  stockNumber: string;
  make: string;
  model: string;
  year: number;
  color: string;
  mileage: number;
  fuelType: FuelType | string;
  askingPriceAmount: number;
  askingPriceCurrency: string;
  status: VehicleStatus | string;
  dateAddedToInventoryUtc: string;
  daysInInventory: number;
  isAging: boolean;
  agingSeverity: AgingSeverity | string;
  demandLevel: DemandLevel | string;
  demandScore: number;
}

export interface AgingStockItem {
  id: string;
  vin: string;
  stockNumber: string;
  make: string;
  model: string;
  year: number;
  daysInInventory: number;
  agingSeverity: AgingSeverity | string;
  askingPriceAmount: number;
  askingPriceCurrency: string;
  demandScore: number;
  demandLevel: DemandLevel | string;
}

export interface LowInventoryAlert {
  id: string;
  make: string;
  model: string;
  available: number;
  threshold: number;
}
