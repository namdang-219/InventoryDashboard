import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Vehicle } from '../../../../core/models/vehicle.model';
import { AgingSeverityBadgeComponent } from '../../../../shared/components/aging-severity-badge/aging-severity-badge.component';
import { DemandBadgeComponent } from '../../../../shared/components/demand-badge/demand-badge.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { FormatCurrencyPipe } from '../../../../shared/pipes/format-currency.pipe';

@Component({
  selector: 'iid-vehicle-card',
  standalone: true,
  imports: [
    StatusBadgeComponent,
    AgingSeverityBadgeComponent,
    DemandBadgeComponent,
    FormatCurrencyPipe
  ],
  templateUrl: './vehicle-card.component.html',
  styleUrl: './vehicle-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VehicleCardComponent {
  readonly vehicle = input.required<Vehicle>();
  readonly isManager = input<boolean>(false);
  readonly canMarkSold = input<boolean>(false);

  readonly viewDetails = output<Vehicle>();
  readonly editVehicle = output<Vehicle>();
  readonly markSold = output<Vehicle>();
  readonly logAction = output<Vehicle>();
  readonly transferDealership = output<Vehicle>();
}
