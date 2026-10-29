import { useEffect, useState } from 'react';
import { Outlet } from 'react-router-dom';
import { realtimeService } from '../../core/services/realtime.service';
import { HeaderComponent } from '../header/header';
import { SidebarComponent } from '../sidebar/sidebar';
import './shell.scss';

export function ShellComponent() {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  useEffect(() => {
    // Automatically establish SignalR hub connection for realtime inventory sync
    realtimeService.startConnection();
  }, []);

  const toggleSidebar = () => {
    setSidebarCollapsed(v => !v);
  };

  return (
    <div className="iid-shell shell-layout">
      <SidebarComponent collapsed={sidebarCollapsed} onToggleCollapse={toggleSidebar} />
      <div className="shell-main">
        <HeaderComponent />
        <main className="shell-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
