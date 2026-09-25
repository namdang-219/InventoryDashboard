import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'iid-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SidebarComponent {
  private readonly auth = inject(AuthService);

  readonly collapsed = input<boolean>(false);
  readonly toggleCollapse = output<void>();

  readonly navItems = computed(() => {
    const items = [
      { label: 'Dashboard', path: '/dashboard', icon: 'dashboard', exact: true }
    ];

    if (this.auth.isManager()) {
      items.push({ label: 'Dealerships', path: '/dealerships', icon: 'storefront', exact: true });
    }

    items.push(
      { label: 'Vehicle Inventory', path: '/vehicles', icon: 'directions_car', exact: true },
      { label: 'Aging Stock', path: '/vehicles/aging', icon: 'hourglass_bottom', exact: false }
    );

    return items;
  });
}
