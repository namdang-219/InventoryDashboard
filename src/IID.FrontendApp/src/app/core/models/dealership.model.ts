export interface Dealership {
  readonly id: string;
  readonly name: string;
  readonly code: string;
  readonly city: string;
  readonly state: string;
  readonly phone: string;
  readonly vehicleCount?: number;
}

export interface CreateDealershipRequest {
  name: string;
  code: string;
  city: string;
  state: string;
  phone: string;
}

export interface UpdateDealershipRequest {
  name: string;
  code: string;
  city: string;
  state: string;
  phone: string;
}
