import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import {
  HubConnectionStatus,
  LiveActivityFeedItem,
  RealtimeDashboardAlertResponse,
  RealtimeDashboardSummaryResponse,
  RealtimeVehicleActionResponse,
  RealtimeVehicleResponse
} from '../models/realtime.model';
import { ApiConfigService } from './api.service';
import { AuthService } from './auth.service';
import { ToastService } from './toast.service';

@Injectable({
  providedIn: 'root'
})
export class RealtimeService {
  private readonly http = inject(HttpClient);
  private readonly apiConfig = inject(ApiConfigService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  private hubConnection: signalR.HubConnection | null = null;

  // Connection State
  readonly connectionStatus = signal<HubConnectionStatus>('disconnected');
  readonly activeDealershipId = signal<string | null>(null);
  readonly recentActivities = signal<readonly LiveActivityFeedItem[]>([]);
  readonly nextCursor = signal<string | null>(null);
  readonly hasMoreActivities = signal<boolean>(false);
  readonly isLoadingActivities = signal<boolean>(false);
  readonly isLoadingMoreActivities = signal<boolean>(false);
  readonly totalActivities = signal<number>(0);
  readonly unreadCount = signal<number>(0);

  // Event Streams
  readonly vehicleAdded$ = new Subject<RealtimeVehicleResponse>();
  readonly vehicleUpdated$ = new Subject<RealtimeVehicleResponse>();
  readonly vehicleRemoved$ = new Subject<string>();
  readonly vehicleAging$ = new Subject<RealtimeVehicleResponse>();
  readonly vehicleActionLogged$ = new Subject<RealtimeVehicleActionResponse>();
  readonly summaryUpdated$ = new Subject<RealtimeDashboardSummaryResponse>();
  readonly alertsUpdated$ = new Subject<readonly RealtimeDashboardAlertResponse[]>();
  readonly inventoryChanged$ = new Subject<void>();

  startConnection(): void {
    if (
      this.hubConnection &&
      (this.hubConnection.state === signalR.HubConnectionState.Connected ||
       this.hubConnection.state === signalR.HubConnectionState.Connecting)
    ) {
      return;
    }

    const hubUrl = this.apiConfig.getHubUrl();
    this.connectionStatus.set('connecting');

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => this.auth.token() ?? ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.registerHandlers();

    this.hubConnection
      .start()
      .then(() => {
        this.connectionStatus.set('connected');
        const activeId = this.activeDealershipId();
        if (activeId) {
          this.hubConnection?.invoke('JoinDealership', activeId).catch(() => {});
        }
        this.loadInitialActivities();
      })
      .catch(() => {
        this.connectionStatus.set('disconnected');
      });

    this.hubConnection.onreconnecting(() => {
      this.connectionStatus.set('reconnecting');
    });

    this.hubConnection.onreconnected(() => {
      this.connectionStatus.set('connected');
      const activeId = this.activeDealershipId();
      if (activeId) {
        this.hubConnection?.invoke('JoinDealership', activeId).catch(() => {});
      }
      this.toast.info('SignalR Reconnected', 'Inventory feed synced.');
    });

    this.hubConnection.onclose(() => {
      this.connectionStatus.set('disconnected');
    });
  }

  async switchDealership(newDealershipId: string, oldDealershipId?: string | null): Promise<void> {
    this.activeDealershipId.set(newDealershipId);
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      if (oldDealershipId && oldDealershipId !== newDealershipId) {
        await this.hubConnection.invoke('LeaveDealership', oldDealershipId);
      }
      if (newDealershipId) {
        await this.hubConnection.invoke('JoinDealership', newDealershipId);
      }
    } catch (err) {
      console.warn('Failed to switch dealership room:', err);
    }
  }

  stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
      this.connectionStatus.set('disconnected');
    }
  }

  private registerHandlers(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('VehicleAdded', (v: RealtimeVehicleResponse) => {
      this.vehicleAdded$.next(v);
      // Toast is handled by the create-vehicle-modal on HTTP success.
      // No duplicate toast here.
    });

    this.hubConnection.on('VehicleUpdated', (v: RealtimeVehicleResponse) => {
      this.vehicleUpdated$.next(v);
      // Live Activity Feed only surfaces action logs — vehicle CRUD events
      // still emit toast + subject for downstream consumers (vehicle list
      // refresh, etc.) but are intentionally not pushed into the feed.
    });

    this.hubConnection.on('VehicleRemoved', (vehicleId: string) => {
      this.vehicleRemoved$.next(vehicleId);
    });

    this.hubConnection.on('VehicleAging', (v: RealtimeVehicleResponse) => {
      this.vehicleAging$.next(v);
      this.toast.warning('Aging Stock Warning', `${v.make} ${v.model} has been on lot for ${v.daysInInventory} days.`);
    });

    this.hubConnection.on('VehicleActionLogged', (a: RealtimeVehicleActionResponse) => {
      this.vehicleActionLogged$.next(a);
      this.addActivity({
        id: a.id,
        type: 'action',
        title: `Action: ${a.actionType}`,
        detail: a.notes ?? 'No notes provided',
        timestamp: new Date(),
        severity: 'info',
        vehicleId: a.vehicleId,
        vehicleName: a.vehicleName ?? undefined,
        isRead: false
      });
    });

    this.hubConnection.on('DashboardSummaryUpdated', (s: RealtimeDashboardSummaryResponse) => {
      this.summaryUpdated$.next(s);
    });

    this.hubConnection.on('DashboardAlertsUpdated', (alerts: RealtimeDashboardAlertResponse[]) => {
      this.alertsUpdated$.next(alerts);
    });

    this.hubConnection.on('InventoryChanged', () => {
      this.inventoryChanged$.next();
    });
  }

  loadInitialActivities(limit = 10): void {
    if (!this.auth.token()) return;
    this.isLoadingActivities.set(true);
    const url = this.apiConfig.getApiUrl(`/api/v1/activities?limit=${limit}`);
    this.http.get<{
      items: { id: string; type: string; title: string; detail: string; timestamp: string; severity?: string; vehicleId?: string; vehicleName?: string; isRead: boolean }[];
      nextCursor: string | null;
      hasMore: boolean;
      total: number;
      unreadCount?: number;
    }>(url).subscribe({
      next: res => {
        this.isLoadingActivities.set(false);
        if (res?.items) {
          const mapped: LiveActivityFeedItem[] = res.items.map(i => ({
            id: i.id,
            type: (i.type || 'action') as any,
            title: i.title,
            detail: i.detail,
            timestamp: new Date(i.timestamp),
            severity: (i.severity || 'info') as any,
            vehicleId: i.vehicleId ?? undefined,
            vehicleName: i.vehicleName ?? undefined,
            isRead: i.isRead
          }));
          this.recentActivities.set(mapped);
          this.nextCursor.set(res.nextCursor ?? null);
          this.hasMoreActivities.set(res.hasMore ?? false);
          this.totalActivities.set(res.total ?? mapped.length);
          this.unreadCount.set(res.unreadCount ?? mapped.filter(i => !i.isRead).length);
        }
      },
      error: () => {
        this.isLoadingActivities.set(false);
      }
    });
  }

  loadMoreActivities(limit = 10): void {
    const cursor = this.nextCursor();
    if (!this.auth.token() || !cursor || this.isLoadingMoreActivities() || !this.hasMoreActivities()) {
      return;
    }

    this.isLoadingMoreActivities.set(true);
    const url = this.apiConfig.getApiUrl(`/api/v1/activities?limit=${limit}&cursor=${encodeURIComponent(cursor)}`);
    this.http.get<{
      items: { id: string; type: string; title: string; detail: string; timestamp: string; severity?: string; vehicleId?: string; vehicleName?: string; isRead: boolean }[];
      nextCursor: string | null;
      hasMore: boolean;
      total: number;
      unreadCount?: number;
    }>(url).subscribe({
      next: res => {
        this.isLoadingMoreActivities.set(false);
        if (res?.items) {
          const mapped: LiveActivityFeedItem[] = res.items.map(i => ({
            id: i.id,
            type: (i.type || 'action') as any,
            title: i.title,
            detail: i.detail,
            timestamp: new Date(i.timestamp),
            severity: (i.severity || 'info') as any,
            vehicleId: i.vehicleId ?? undefined,
            vehicleName: i.vehicleName ?? undefined,
            isRead: i.isRead
          }));

          const existingIds = new Set(this.recentActivities().map(a => a.id));
          const newItems = mapped.filter(item => !existingIds.has(item.id));

          this.recentActivities.update(curr => [...curr, ...newItems]);
          this.nextCursor.set(res.nextCursor ?? null);
          this.hasMoreActivities.set(res.hasMore ?? false);
          if (res.total !== undefined) {
            this.totalActivities.set(res.total);
          }
          if (res.unreadCount !== undefined) {
            this.unreadCount.set(res.unreadCount);
          }
        }
      },
      error: () => {
        this.isLoadingMoreActivities.set(false);
      }
    });
  }

  markAsRead(activityId: string): void {
    const target = this.recentActivities().find(item => item.id === activityId);
    if (target && !target.isRead) {
      this.unreadCount.update(c => Math.max(0, c - 1));
    }

    this.recentActivities.update(items =>
      items.map(item => item.id === activityId ? { ...item, isRead: true } : item)
    );
    const url = this.apiConfig.getApiUrl('/api/v1/activities/read');
    this.http.post(url, { activityIds: [activityId] }).subscribe({
      error: () => {}
    });
  }

  markAllAsRead(): void {
    this.unreadCount.set(0);
    this.recentActivities.update(items =>
      items.map(item => ({ ...item, isRead: true }))
    );
    const url = this.apiConfig.getApiUrl('/api/v1/activities/read');
    this.http.post(url, { all: true }).subscribe({
      error: () => {}
    });
  }

  private addActivity(item: LiveActivityFeedItem): void {
    if (this.recentActivities().some(existing => existing.id === item.id)) {
      return;
    }
    this.recentActivities.update(list => [item, ...list]);
    this.totalActivities.update(t => t + 1);
    if (!item.isRead) {
      this.unreadCount.update(c => c + 1);
    }
  }
}
