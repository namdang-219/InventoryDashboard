import clsx from 'clsx';
import { useMemo } from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../../core/services/auth.service';
import './sidebar.scss';

export interface SidebarProps {
  collapsed?: boolean;
  onToggleCollapse?: () => void;
}

export function SidebarComponent({ collapsed = false, onToggleCollapse }: SidebarProps) {
  const { isManager } = useAuth();

  const navItems = useMemo(() => {
    const items = [{ label: 'Dashboard', path: '/dashboard', icon: 'dashboard', exact: true }];

    if (isManager) {
      items.push({ label: 'Dealerships', path: '/dealerships', icon: 'storefront', exact: true });
    }

    items.push(
      { label: 'Vehicle Inventory', path: '/vehicles', icon: 'directions_car', exact: true },
      { label: 'Aging Stock', path: '/vehicles/aging', icon: 'hourglass_bottom', exact: false }
    );

    return items;
  }, [isManager]);

  return (
    <aside className={clsx('iid-sidebar', 'sidebar', collapsed && 'collapsed')}>
      <div className="sidebar-header">
        <div className="brand">
          <div className="logo-mark">
            <span className="material-symbols-outlined logo-icon">electric_car</span>
          </div>
          {!collapsed && (
            <div className="brand-text">
              <span className="brand-name">IID System</span>
              <span className="brand-tag">Intelligent Inventory</span>
            </div>
          )}
        </div>
        <button
          type="button"
          className="collapse-btn"
          onClick={() => onToggleCollapse?.()}
          title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        >
          <span className="material-symbols-outlined">
            {collapsed ? 'chevron_right' : 'chevron_left'}
          </span>
        </button>
      </div>

      <nav className="sidebar-nav">
        <div className="nav-section-title">{!collapsed && <span>Main Menu</span>}</div>
        <ul className="nav-list">
          {navItems.map(item => (
            <li key={item.path}>
              <NavLink
                to={item.path}
                end={item.exact}
                className={({ isActive }) => clsx('nav-link', isActive && 'active')}
                title={item.label}
              >
                <span className="material-symbols-outlined nav-icon">{item.icon}</span>
                {!collapsed && <span className="nav-label">{item.label}</span>}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      <div className="sidebar-footer">
        {!collapsed && (
          <div className="status-summary">
            <div className="indicator-dot" />
            <div className="summary-text">
              <span className="summary-title">Live Engine</span>
              <span className="summary-sub">SignalR Connected</span>
            </div>
          </div>
        )}
      </div>
    </aside>
  );
}
