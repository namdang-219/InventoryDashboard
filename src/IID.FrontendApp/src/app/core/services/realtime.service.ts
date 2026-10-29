import * as signalR from '@microsoft/signalr';
import { useEffect, useRef } from 'react';
import {
  HubConnectionStatus,
  LiveActivityFeedItem,
  RealtimeDashboardAlertResponse,
  RealtimeDashboardSummaryResponse,
  RealtimeDealershipResponse,
  RealtimeVehicleActionResponse,
  RealtimeVehicleResponse
} from '../models/realtime.model';
import { useAppSelector } from '../store/hooks';
import {
  activeDealershipSet,
  activitiesLoaded,
  activitiesLoading,
  activityAdded,
  activityMarkedRead,
  activityTicked,
  allActivitiesMarkedRead,
  connectionStatusSet,
  moreActivitiesLoaded,
  moreActivitiesLoading
} from '../store/realtime.slice';
import { store } from '../store/store';
import { apiConfig, get, post } from './api.service';
import { toastService } from './toast.service';

/** Event streams (replacement for the RxJS Subjects of the Angular service). */
export interface RealtimeEventMap {
  vehicleAdded: RealtimeVehicleResponse;
  vehicleUpdated: RealtimeVehicleResponse;
  vehicleRemoved: string;
  vehicleAging: RealtimeVehicleResponse;
  vehicleActionLogged: RealtimeVehicleActionResponse;
  summaryUpdated: RealtimeDashboardSummaryResponse;
  alertsUpdated: readonly RealtimeDashboardAlertResponse[];
  inventoryChanged: void;
  dealershipAdded: RealtimeDealershipResponse;
  dealershipUpdated: RealtimeDealershipResponse;
}

type Handler<K extends keyof RealtimeEventMap> = (payload: RealtimeEventMap[K]) => void;

class RealtimeEvents {
  private handlers = new Map<keyof RealtimeEventMap, Set<Handler<never>>>();

  /** Subscribes to an event; returns the unsubscribe function. */
  on<K extends keyof RealtimeEventMap>(event: K, handler: Handler<K>): () => void {
    let set = this.handlers.get(event);
    if (!set) {
      set = new Set();
      this.handlers.set(event, set);
    }
    set.add(handler as Handler<never>);
    return () => {
      set.delete(handler as Handler<never>);
    };
  }

  emit<K extends keyof RealtimeEventMap>(event: K, payload: RealtimeEventMap[K]): void {
    this.handlers.get(event)?.forEach(h => (h as Handler<K>)(payload));
  }
}

export const realtimeEvents = new RealtimeEvents();

interface ActivitiesResponse {
  items: {
    id: string;
    type: string;
    title: string;
    detail: string;
    timestamp: string;
    severity?: string;
    vehicleId?: string;
    vehicleName?: string;
    isRead: boolean;
  }[];
  nextCursor: string | null;
  hasMore: boolean;
  total: number;
  unreadCount?: number;
}

function mapActivities(items: ActivitiesResponse['items']): LiveActivityFeedItem[] {
  return items.map(i => ({
    id: i.id,
    type: (i.type || 'action') as LiveActivityFeedItem['type'],
    title: i.title,
    detail: i.detail,
    timestamp: new Date(i.timestamp),
    severity: (i.severity || 'info') as LiveActivityFeedItem['severity'],
    vehicleId: i.vehicleId ?? undefined,
    vehicleName: i.vehicleName ?? undefined,
    isRead: i.isRead
  }));
}

let hubConnection: signalR.HubConnection | null = null;

const state = () => store.getState().realtime;
const dispatch = store.dispatch;

export const realtimeService = {
  startConnection(): void {
    if (
      hubConnection &&
      (hubConnection.state === signalR.HubConnectionState.Connected ||
        hubConnection.state === signalR.HubConnectionState.Connecting ||
        hubConnection.state === signalR.HubConnectionState.Reconnecting)
    ) {
      return;
    }

    const hubUrl = apiConfig.getHubUrl();
    dispatch(connectionStatusSet('connecting'));

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => store.getState().auth.token ?? ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();
    hubConnection = connection;

    registerHandlers(connection);

    connection
      .start()
      .then(() => {
        if (hubConnection !== connection) return;
        dispatch(connectionStatusSet('connected'));
        const activeId = state().activeDealershipId;
        if (activeId) {
          connection.invoke('JoinDealership', activeId).catch(() => {});
        }
        realtimeService.loadInitialActivities();
      })
      .catch(() => {
        if (hubConnection !== connection) return;
        dispatch(connectionStatusSet('disconnected'));
      });

    connection.onreconnecting(() => {
      dispatch(connectionStatusSet('reconnecting'));
    });

    connection.onreconnected(() => {
      dispatch(connectionStatusSet('connected'));
      const activeId = state().activeDealershipId;
      if (activeId) {
        connection.invoke('JoinDealership', activeId).catch(() => {});
      }
      toastService.info('SignalR Reconnected', 'Inventory feed synced.');
    });

    connection.onclose(() => {
      if (hubConnection !== connection) return;
      dispatch(connectionStatusSet('disconnected'));
    });
  },

  async switchDealership(newDealershipId: string, oldDealershipId?: string | null): Promise<void> {
    dispatch(activeDealershipSet(newDealershipId));
    if (!hubConnection || hubConnection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      if (oldDealershipId && oldDealershipId !== newDealershipId) {
        await hubConnection.invoke('LeaveDealership', oldDealershipId);
      }
      if (newDealershipId) {
        await hubConnection.invoke('JoinDealership', newDealershipId);
      }
    } catch (err) {
      console.warn('Failed to switch dealership room:', err);
    }
  },

  stopConnection(): void {
    if (hubConnection) {
      const connection = hubConnection;
      hubConnection = null;
      void connection.stop();
      dispatch(connectionStatusSet('disconnected'));
    }
  },

  loadInitialActivities(limit = 10): void {
    if (!store.getState().auth.token) return;
    dispatch(activitiesLoading(true));
    get<ActivitiesResponse>(apiConfig.getApiUrl(`/api/v1/activities?limit=${limit}`))
      .then(res => {
        dispatch(activitiesLoading(false));
        if (res?.items) {
          const mapped = mapActivities(res.items);
          dispatch(
            activitiesLoaded({
              items: mapped,
              nextCursor: res.nextCursor ?? null,
              hasMore: res.hasMore ?? false,
              total: res.total ?? mapped.length,
              unreadCount: res.unreadCount ?? mapped.filter(i => !i.isRead).length
            })
          );
        }
      })
      .catch(() => {
        dispatch(activitiesLoading(false));
      });
  },

  loadMoreActivities(limit = 10): void {
    const { nextCursor: cursor, isLoadingMoreActivities, hasMoreActivities } = state();
    if (!store.getState().auth.token || !cursor || isLoadingMoreActivities || !hasMoreActivities) {
      return;
    }

    dispatch(moreActivitiesLoading(true));
    get<ActivitiesResponse>(
      apiConfig.getApiUrl(`/api/v1/activities?limit=${limit}&cursor=${encodeURIComponent(cursor)}`)
    )
      .then(res => {
        dispatch(moreActivitiesLoading(false));
        if (res?.items) {
          dispatch(
            moreActivitiesLoaded({
              items: mapActivities(res.items),
              nextCursor: res.nextCursor ?? null,
              hasMore: res.hasMore ?? false,
              total: res.total,
              unreadCount: res.unreadCount
            })
          );
        }
      })
      .catch(() => {
        dispatch(moreActivitiesLoading(false));
      });
  },

  markAsRead(activityId: string): void {
    dispatch(activityMarkedRead(activityId));
    post(apiConfig.getApiUrl('/api/v1/activities/read'), { activityIds: [activityId] }).catch(() => {});
  },

  markAllAsRead(): void {
    dispatch(allActivitiesMarkedRead());
    post(apiConfig.getApiUrl('/api/v1/activities/read'), { all: true }).catch(() => {});
  }
};

function registerHandlers(connection: signalR.HubConnection): void {
  connection.on('VehicleAdded', (v: RealtimeVehicleResponse) => {
    realtimeEvents.emit('vehicleAdded', v);
    // Toast is handled by the create-vehicle-modal on HTTP success.
    // No duplicate toast here.
  });

  connection.on('VehicleUpdated', (v: RealtimeVehicleResponse) => {
    realtimeEvents.emit('vehicleUpdated', v);
    // Live Activity Feed only surfaces action logs — vehicle CRUD events
    // still emit for downstream consumers (vehicle list refresh, etc.)
    // but are intentionally not pushed into the feed.
  });

  connection.on('VehicleRemoved', (vehicleId: string) => {
    realtimeEvents.emit('vehicleRemoved', vehicleId);
  });

  connection.on('VehicleAging', (v: RealtimeVehicleResponse) => {
    realtimeEvents.emit('vehicleAging', v);
    toastService.warning('Aging Stock Warning', `${v.make} ${v.model} has been on lot for ${v.daysInInventory} days.`);
    dispatch(activityTicked());
  });

  connection.on('VehicleActionLogged', (a: RealtimeVehicleActionResponse) => {
    realtimeEvents.emit('vehicleActionLogged', a);
    dispatch(
      activityAdded({
        id: a.id,
        type: 'action',
        title: `Action: ${a.actionType}`,
        detail: a.notes ?? 'No notes provided',
        timestamp: new Date(),
        severity: 'info',
        vehicleId: a.vehicleId,
        vehicleName: a.vehicleName ?? undefined,
        isRead: false
      })
    );
    dispatch(activityTicked());
  });

  connection.on('DashboardSummaryUpdated', (s: RealtimeDashboardSummaryResponse) => {
    realtimeEvents.emit('summaryUpdated', s);
  });

  connection.on('DashboardAlertsUpdated', (alerts: RealtimeDashboardAlertResponse[]) => {
    realtimeEvents.emit('alertsUpdated', alerts);
  });

  connection.on('InventoryChanged', () => {
    realtimeEvents.emit('inventoryChanged', undefined);
  });

  connection.on('DealershipAdded', (d: RealtimeDealershipResponse) => {
    realtimeEvents.emit('dealershipAdded', d);
    // Dealership addition updates the dealership dropdown reactively.
    // Intentionally not pushed into the Live Activity Feed or notification bell.
  });

  connection.on('DealershipUpdated', (d: RealtimeDealershipResponse) => {
    realtimeEvents.emit('dealershipUpdated', d);
    // Dealership update updates the dealership dropdown reactively.
    // Intentionally not pushed into the Live Activity Feed or notification bell.
  });
}

/** Subscribe a component to a realtime event for its lifetime. */
export function useRealtimeEvent<K extends keyof RealtimeEventMap>(event: K, handler: Handler<K>): void {
  const ref = useRef(handler);
  ref.current = handler;
  useEffect(() => realtimeEvents.on(event, payload => ref.current(payload)), [event]);
}

export function useRealtime() {
  const s = useAppSelector(st => st.realtime);
  return { ...s, ...realtimeService };
}

export type { HubConnectionStatus };
