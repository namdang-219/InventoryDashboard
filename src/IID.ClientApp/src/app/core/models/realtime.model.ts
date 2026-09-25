export type HubConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

export interface RealtimeVehicleResponse {
  id: string;
  vin: string;
  make: string;
  model: string;
  year: number;
  daysInInventory: number;
  isAging: boolean;
  status: string;
}

export interface RealtimeVehicleActionResponse {
  id: string;
  vehicleId: string;
  actionType: string;
  notes?: string | null;
  loggedAtUtc: string;
  vehicleName?: string | null;
}

export interface RealtimeDashboardSummaryResponse {
  generatedAtUtc: string;
  totalInventory: number;
  availableCount: number;
  pendingCount: number;
  soldCount: number;
  wholesaleCount: number;
  agingCount: number;
  totalInventoryValue: number;
}

export interface RealtimeDashboardAlertResponse {
  vehicleId?: string | null;
  message: string;
  severity: string;
  createdAtUtc: string;
}

export interface LiveActivityFeedItem {
  id: string;
  type: 'added' | 'updated' | 'removed' | 'aging' | 'action' | 'alert';
  title: string;
  detail: string;
  timestamp: Date;
  severity?: 'info' | 'success' | 'warning' | 'danger';
  vehicleId?: string;
  vehicleName?: string;
  isRead?: boolean;
}
