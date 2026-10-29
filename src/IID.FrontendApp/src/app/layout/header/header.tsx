import clsx from 'clsx';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { LiveActivityFeedItem } from '../../core/models/realtime.model';
import { Vehicle } from '../../core/models/vehicle.model';
import { useAuth } from '../../core/services/auth.service';
import { useDealerships } from '../../core/services/dealership.service';
import { inventoryService } from '../../core/services/inventory.service';
import { realtimeService, useRealtime } from '../../core/services/realtime.service';
import { useTheme } from '../../core/services/theme.service';
import { toastService } from '../../core/services/toast.service';
import { EditVehicleModalComponent } from '../../features/vehicles/components/edit-vehicle-modal/edit-vehicle-modal';
import { LogActionModalComponent } from '../../features/vehicles/components/log-action-modal/log-action-modal';
import { MarkSoldModalComponent } from '../../features/vehicles/components/mark-sold-modal/mark-sold-modal';
import { VehicleDetailModalComponent } from '../../features/vehicles/components/vehicle-detail-modal/vehicle-detail-modal';
import './header.scss';

export function HeaderComponent() {
  const { currentUser: user, token, isManager, canMarkSold, logout: authLogout } = useAuth();
  const realtime = useRealtime();
  const theme = useTheme();
  const dealership = useDealerships();
  const navigate = useNavigate();

  const [bellRinging, setBellRinging] = useState(false);
  const ringTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const lastTickRef = useRef<number>(realtime.latestActivityTick);

  const [showNotifications, setShowNotifications] = useState(false);
  const [selectedVehicleForDetail, setSelectedVehicleForDetail] = useState<Vehicle | null>(null);
  const [highlightedActionId, setHighlightedActionId] = useState<string | null>(null);
  const [selectedVehicleForAction, setSelectedVehicleForAction] = useState<Vehicle | null>(null);
  const [selectedVehicleForEdit, setSelectedVehicleForEdit] = useState<Vehicle | null>(null);
  const [selectedVehicleForSold, setSelectedVehicleForSold] = useState<Vehicle | null>(null);

  // Watch for new realtime activities (actions / aging warnings) and ring the bell.
  useEffect(() => {
    if (realtime.latestActivityTick === lastTickRef.current) {
      return;
    }
    lastTickRef.current = realtime.latestActivityTick;

    // Restart the 5s window on each new activity so a burst of notifications
    // rings the bell continuously rather than clipping short.
    if (ringTimeoutRef.current !== null) {
      clearTimeout(ringTimeoutRef.current);
    }
    setBellRinging(true);
    ringTimeoutRef.current = setTimeout(() => {
      setBellRinging(false);
      ringTimeoutRef.current = null;
    }, 5000);
  }, [realtime.latestActivityTick]);

  useEffect(() => {
    return () => {
      if (ringTimeoutRef.current !== null) {
        clearTimeout(ringTimeoutRef.current);
      }
    };
  }, []);

  useEffect(() => {
    if (token) {
      realtimeService.loadInitialActivities(10);
    }
  }, [token]);

  const {
    connectionStatus,
    recentActivities: activities,
    hasMoreActivities,
    isLoadingMoreActivities,
    totalActivities
  } = realtime;

  const unreadCount = useMemo(() => {
    return Math.max(realtime.unreadCount, activities.filter(a => !a.isRead).length);
  }, [realtime.unreadCount, activities]);

  const unreadBadgeText = useMemo(() => {
    return unreadCount >= 10 ? '10+' : unreadCount.toString();
  }, [unreadCount]);

  const onDealershipSelect = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const val = event.target.value;
    if (val) {
      dealership.selectDealership(val);
    }
  };

  const toggleNotifications = () => {
    setShowNotifications(prev => {
      const opening = !prev;
      if (opening && activities.length === 0) {
        realtimeService.loadInitialActivities(10);
      }
      return opening;
    });
  };

  const onDropdownScroll = (event: React.UIEvent<HTMLDivElement>) => {
    const el = event.currentTarget;
    const threshold = 40;
    const atBottom = el.scrollHeight - el.scrollTop - el.clientHeight <= threshold;
    if (atBottom && hasMoreActivities && !isLoadingMoreActivities) {
      realtimeService.loadMoreActivities(10);
    }
  };

  const markAllAsRead = () => {
    realtimeService.markAllAsRead();
  };

  const onActivityClick = (act: LiveActivityFeedItem) => {
    // Mark as read immediately on click
    if (!act.isRead) {
      realtimeService.markAsRead(act.id);
    }

    if (act.dealershipId) {
      setShowNotifications(false);
      dealership.selectDealership(act.dealershipId);
      if (isManager) {
        navigate('/dealerships');
      }
      return;
    }

    if (act.vehicleId && act.type !== 'removed') {
      setShowNotifications(false);
      inventoryService
        .getVehicleById(act.vehicleId)
        .then(res => {
          if (act.type === 'action') {
            setHighlightedActionId(act.id);
          } else {
            setHighlightedActionId(null);
          }
          setSelectedVehicleForDetail(res.data);
        })
        .catch(() => {
          toastService.error('Vehicle Not Found', 'Could not load vehicle details.');
        });
    }
  };

  const closeDetailModal = () => {
    setSelectedVehicleForDetail(null);
    setHighlightedActionId(null);
  };

  const onEditFromDetail = (v: Vehicle) => {
    closeDetailModal();
    setSelectedVehicleForEdit(v);
  };

  const onLogActionFromDetail = (v: Vehicle) => {
    closeDetailModal();
    setSelectedVehicleForAction(v);
  };

  const onMarkSoldFromDetail = (v: Vehicle) => {
    if (!canMarkSold) {
      toastService.warning('Permission Denied', 'Only Sales can mark vehicles as sold.');
      return;
    }
    closeDetailModal();
    setSelectedVehicleForSold(v);
  };

  const closeActionModal = () => {
    setSelectedVehicleForAction(null);
  };

  const closeEditModal = () => {
    setSelectedVehicleForEdit(null);
  };

  const closeSoldModal = () => {
    setSelectedVehicleForSold(null);
  };

  const logout = () => {
    realtimeService.stopConnection();
    authLogout();
  };

  return (
    <>
      <header className="iid-header header">
        <div className="header-left">
          <div className="header-title-wrap">
            <h1 className="header-title">Intelligent Inventory Dashboard</h1>
            <span className="header-subtitle">Real-time Automotive Asset Tracking</span>
          </div>
        </div>

        <div className="header-right">
          {/* Dealership Selector Dropdown */}
          <div className="dealership-selector-pill">
            <span className="material-symbols-outlined dealership-icon">storefront</span>
            <div className="dealership-select-wrapper">
              <label htmlFor="dealership-select" className="dealership-label">
                Dealership
              </label>
              <select
                id="dealership-select"
                className="dealership-dropdown"
                value={dealership.selectedDealershipId ?? ''}
                onChange={onDealershipSelect}
                title="Select Active Dealership"
              >
                {dealership.dealerships.map(d => (
                  <option key={d.id} value={d.id}>
                    {d.name} — {d.city}, {d.state}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* SignalR Status Indicator */}
          <div className={clsx('status-pill', `conn-${connectionStatus}`)}>
            <span className="dot" />
            <span className="status-label">
              {connectionStatus === 'connected'
                ? 'Live Sync'
                : connectionStatus === 'connecting'
                  ? 'Connecting...'
                  : connectionStatus === 'reconnecting'
                    ? 'Reconnecting...'
                    : 'Offline'}
            </span>
            {connectionStatus === 'disconnected' && (
              <button
                type="button"
                className="reconnect-btn"
                onClick={() => realtimeService.startConnection()}
                title="Reconnect SignalR"
              >
                <span className="material-symbols-outlined">refresh</span>
              </button>
            )}
          </div>

          {/* Theme Toggle Mode (Dark / Light) */}
          <button
            type="button"
            className="theme-toggle-btn"
            onClick={() => theme.toggleTheme()}
            title={theme.isDark ? 'Switch to Light mode' : 'Switch to Dark mode'}
            aria-label={theme.isDark ? 'Switch to Light mode' : 'Switch to Dark mode'}
          >
            <span className="material-symbols-outlined theme-icon">
              {theme.isDark ? 'dark_mode' : 'light_mode'}
            </span>
            <span className="theme-text">{theme.isDark ? 'Dark' : 'Light'}</span>
          </button>

          {/* Notification Bell */}
          <div className="notification-wrap">
            <button
              type="button"
              className={clsx('iid-btn-icon', 'bell-btn', bellRinging && 'ringing')}
              onClick={toggleNotifications}
              title="Live Alerts"
            >
              <span className="material-symbols-outlined bell-icon">notifications</span>
              {unreadCount > 0 && <span className="badge-count">{unreadBadgeText}</span>}
            </button>

            {showNotifications && (
              <div className="notification-dropdown iid-card">
                <div className="dropdown-header">
                  <div className="header-feed-info">
                    <h4>Live Activity Feed</h4>
                    <span className="activity-count">
                      {totalActivities || activities.length} events
                    </span>
                  </div>
                  {unreadCount > 0 && (
                    <button
                      type="button"
                      className="mark-all-read-btn"
                      onClick={markAllAsRead}
                      title="Mark all as read"
                    >
                      <span className="material-symbols-outlined">done_all</span>
                      <span>Mark all read</span>
                    </button>
                  )}
                </div>

                <div className="dropdown-body" onScroll={onDropdownScroll}>
                  {activities.length === 0 ? (
                    <div className="empty-feed">
                      <span className="material-symbols-outlined">notifications_off</span>
                      <span>No live events yet</span>
                    </div>
                  ) : (
                    <>
                      <ul className="activity-list">
                        {activities.map(act => {
                          const clickable =
                            (!!act.vehicleId && act.type !== 'removed') ||
                            (!!act.dealershipId && isManager);
                          const timeStr = (
                            act.timestamp instanceof Date ? act.timestamp : new Date(act.timestamp)
                          ).toLocaleTimeString();

                          return (
                            <li
                              key={act.id}
                              className={clsx(
                                'activity-item',
                                `sev-${act.severity || 'info'}`,
                                !act.isRead && 'unread',
                                clickable && 'clickable'
                              )}
                              onClick={() => onActivityClick(act)}
                            >
                              <div className="activity-icon">
                                {act.type === 'added' ? (
                                  <span className="material-symbols-outlined">add_circle</span>
                                ) : act.type === 'updated' ? (
                                  <span className="material-symbols-outlined">edit</span>
                                ) : act.type === 'removed' ? (
                                  <span className="material-symbols-outlined">delete</span>
                                ) : act.type === 'aging' ? (
                                  <span className="material-symbols-outlined">warning</span>
                                ) : (
                                  <span className="material-symbols-outlined">info</span>
                                )}
                              </div>
                              <div className="activity-content">
                                <div className="activity-top">
                                  <span className="activity-title">{act.title}</span>
                                  <span className="activity-time">{timeStr}</span>
                                </div>
                                {act.vehicleName && (
                                  <div className="activity-vehicle">
                                    <span className="material-symbols-outlined activity-car-icon">
                                      directions_car
                                    </span>
                                    <span className="activity-car-name">{act.vehicleName}</span>
                                  </div>
                                )}
                                {act.dealershipName && (
                                  <div className="activity-vehicle">
                                    <span className="material-symbols-outlined activity-car-icon">
                                      storefront
                                    </span>
                                    <span className="activity-car-name">{act.dealershipName}</span>
                                  </div>
                                )}
                                <p className="activity-detail">{act.detail}</p>
                                {act.vehicleId && act.type !== 'removed' && (
                                  <div className="activity-action-hint">
                                    <span className="material-symbols-outlined">visibility</span>
                                    <span>Click to view vehicle details</span>
                                  </div>
                                )}
                                {act.dealershipId && isManager && (
                                  <div className="activity-action-hint">
                                    <span className="material-symbols-outlined">storefront</span>
                                    <span>Click to view in Dealership Network</span>
                                  </div>
                                )}
                              </div>
                              {!act.isRead && (
                                <div className="activity-unread-indicator">
                                  <span className="unread-dot" title="Unread" />
                                </div>
                              )}
                            </li>
                          );
                        })}
                      </ul>

                      {isLoadingMoreActivities ? (
                        <div className="feed-loading-more">
                          <span className="material-symbols-outlined spin">progress_activity</span>
                          <span>Loading more activities...</span>
                        </div>
                      ) : hasMoreActivities ? (
                        <div
                          className="feed-load-more-hint"
                          onClick={() => realtimeService.loadMoreActivities(10)}
                        >
                          <span>
                            Scroll or tap to load more ({activities.length} of {totalActivities})
                          </span>
                        </div>
                      ) : activities.length >= 10 ? (
                        <div className="feed-end">
                          <span>All {totalActivities} events loaded</span>
                        </div>
                      ) : null}
                    </>
                  )}
                </div>
              </div>
            )}
          </div>

          {/* User Profile & Logout */}
          {user && (
            <div className="user-profile">
              <div className="user-avatar">
                <span>{user.email.substring(0, 2).toUpperCase()}</span>
              </div>
              <div className="user-info">
                <span className="user-email">{user.email}</span>
                <span className={clsx('user-role', isManager && 'manager')}>
                  {isManager ? 'Manager (Full Access)' : 'Sales'}
                </span>
              </div>
            </div>
          )}

          <button
            type="button"
            className="iid-btn iid-btn-secondary iid-btn-sm"
            onClick={logout}
            title="Sign Out"
          >
            <span className="material-symbols-outlined">logout</span>
            <span>Logout</span>
          </button>
        </div>
      </header>

      {/* Vehicle Detail Modal */}
      {selectedVehicleForDetail && (
        <VehicleDetailModalComponent
          vehicle={selectedVehicleForDetail}
          isManager={isManager}
          canMarkSold={canMarkSold}
          highlightActionId={highlightedActionId}
          onClose={closeDetailModal}
          onEditVehicle={onEditFromDetail}
          onLogAction={onLogActionFromDetail}
          onMarkSold={onMarkSoldFromDetail}
        />
      )}

      {/* Log Action Modal */}
      {selectedVehicleForAction && (
        <LogActionModalComponent
          vehicle={selectedVehicleForAction}
          onClose={closeActionModal}
        />
      )}

      {/* Edit Vehicle Modal */}
      {selectedVehicleForEdit && (
        <EditVehicleModalComponent
          vehicle={selectedVehicleForEdit}
          onClose={closeEditModal}
        />
      )}

      {/* Mark Sold Modal */}
      {selectedVehicleForSold && (
        <MarkSoldModalComponent
          vehicle={selectedVehicleForSold}
          isManager={isManager}
          onClose={closeSoldModal}
        />
      )}
    </>
  );
}
