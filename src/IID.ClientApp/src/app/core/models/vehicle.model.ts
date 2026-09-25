export type VehicleStatus = 'Available' | 'Sold' | 'Pending' | 'Wholesale';

export type FuelType = 'Petrol' | 'Diesel' | 'Electric' | 'Hybrid' | 'PluginHybrid';

export type AgingSeverity = 'None' | 'Warning' | 'High' | 'Critical';

export type DemandLevel = 'Low' | 'Medium' | 'High';

export type VehicleActionType =
  | 'PriceReductionPlanned'
  | 'PriceReductionExecuted'
  | 'TransferToWholesale'
  | 'TradeInCustomer'
  | 'MarketingCampaign'
  | 'DealerAuction'
  | 'ManagerReview'
  | 'Relist'
  | 'TransferDealership'
  | 'Other';

export interface TransferVehicleRequest {
  targetDealershipId: string;
  notes?: string;
}

export interface VehicleAction {
  readonly id: string;
  readonly vehicleId: string;
  readonly actionType: VehicleActionType | string;
  readonly notes?: string | null;
  readonly loggedByUserId: string;
  readonly loggedByName?: string;
  readonly loggedAtUtc: string;
  readonly createdAtUtc?: string;
}

export interface Vehicle {
  readonly id: string;
  readonly vin: string;
  readonly stockNumber: string;
  readonly make: string;
  readonly model: string;
  readonly year: number;
  readonly color: string;
  readonly mileage: number;
  readonly fuelType: FuelType;
  readonly purchasePriceAmount: number;
  readonly purchasePriceCurrency: string;
  readonly askingPriceAmount: number;
  readonly askingPriceCurrency: string;
  readonly soldPriceAmount?: number | null;
  readonly soldPriceCurrency?: string | null;
  readonly soldAtUtc?: string | null;
  readonly grossProfitAmount?: number | null;
  readonly grossMarginPercent?: number | null;
  readonly status: VehicleStatus;
  readonly dateAddedToInventoryUtc: string;
  readonly daysInInventory: number;
  readonly isAging: boolean;
  readonly agingSeverity: AgingSeverity;
  readonly demandLevel: DemandLevel;
  readonly demandScore: number;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
  readonly rowVersion: string;
  readonly dealershipId?: string | null;
  readonly dealershipName?: string | null;
}

export interface CreateVehicleRequest {
  dealershipId?: string;
  vin: string;
  stockNumber?: string | null;
  make: string;
  model: string;
  year: number;
  color: string;
  mileage: number;
  fuelType: FuelType;
  purchasePrice: number;
  askingPrice: number;
  status: VehicleStatus;
  dateAddedToInventory: string;
}

export interface UpdateVehicleRequest {
  make: string;
  model: string;
  year: number;
  color: string;
  mileage: number;
  fuelType: FuelType;
  purchasePrice: number;
  askingPrice: number;
  status: VehicleStatus;
  rowVersion: string;
}

export interface MarkVehicleSoldRequest {
  soldPrice: number;
  soldPriceCurrency?: string;
  soldAtUtc?: string;
  rowVersion: string;
}

export interface LogVehicleActionRequest {
  actionType: VehicleActionType;
  notes?: string;
}

export interface VehicleFilterParams {
  dealershipId?: string;
  make?: string;
  model?: string;
  minAgeDays?: number;
  maxAgeDays?: number;
  status?: VehicleStatus;
  page?: number;
  limit?: number;
  sort?: string;
  order?: 'asc' | 'desc';
}
