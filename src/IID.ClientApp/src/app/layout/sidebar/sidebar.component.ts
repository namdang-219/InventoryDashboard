import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'iid-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SidebarComponent {
  readonly collapsed = input<boolean>(false);
  readonly toggleCollapse = output<void>();

  readonly navItems = [
    { label: 'Dashboard', path: '/dashboard', icon: 'dashboard', exact: true },
    { label: 'Dealerships', path: '/dealerships', icon: 'storefront', exact: true },
    { label: 'Vehicle Inventory', path: '/vehicles', icon: 'directions_car', exact: true },
    { label: 'Aging Stock', path: '/vehicles/aging', icon: 'hourglass_bottom', exact: false }
  ];
}
