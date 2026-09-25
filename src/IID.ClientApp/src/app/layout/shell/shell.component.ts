import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { RealtimeService } from '../../core/services/realtime.service';
import { HeaderComponent } from '../header/header.component';
import { SidebarComponent } from '../sidebar/sidebar.component';

@Component({
  selector: 'iid-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, HeaderComponent],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShellComponent implements OnInit {
  private readonly realtime = inject(RealtimeService);
  readonly sidebarCollapsed = signal<boolean>(false);

  ngOnInit(): void {
    // Automatically establish SignalR hub connection for realtime inventory sync
    this.realtime.startConnection();
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }
}
