import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResponse } from '../models/api-response.model';
import {
  CreateVehicleRequest,
  LogVehicleActionRequest,
  MarkVehicleSoldRequest,
  TransferVehicleRequest,
  UpdateVehicleRequest,
  Vehicle,
  VehicleAction,
  VehicleFilterParams
} from '../models/vehicle.model';
import { ApiConfigService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly apiConfig = inject(ApiConfigService);

  /**
   * GET /api/v1/vehicles
   * Paginated, filterable, sortable vehicle listing
   */
  listVehicles(params: VehicleFilterParams = {}): Observable<PagedResponse<Vehicle>> {
    let httpParams = new HttpParams();

    if (params.dealershipId) httpParams = httpParams.set('dealershipId', params.dealershipId);
    if (params.make) httpParams = httpParams.set('make', params.make);
    if (params.model) httpParams = httpParams.set('model', params.model);
    if (params.minAgeDays !== undefined && params.minAgeDays !== null) {
      httpParams = httpParams.set('minAgeDays', params.minAgeDays.toString());
    }
    if (params.maxAgeDays !== undefined && params.maxAgeDays !== null) {
      httpParams = httpParams.set('maxAgeDays', params.maxAgeDays.toString());
    }
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.limit) httpParams = httpParams.set('limit', params.limit.toString());
    if (params.sort) httpParams = httpParams.set('sort', params.sort);
    if (params.order) httpParams = httpParams.set('order', params.order);

    const url = this.apiConfig.getApiUrl('/api/v1/vehicles');
    return this.http.get<PagedResponse<Vehicle>>(url, { params: httpParams });
  }

  /**
   * GET /api/v1/vehicles/{id}
   * Retrieves single vehicle details
   */
  getVehicleById(id: string): Observable<{ data: Vehicle }> {
    const url = this.apiConfig.getApiUrl(`/api/v1/vehicles/${id}`);
    return this.http.get<{ data: Vehicle }>(url);
  }

  /**
   * POST /api/v1/vehicles
   * Creates a new vehicle in inventory
   */
  createVehicle(request: CreateVehicleRequest): Observable<{ id: string }> {
    const url = this.apiConfig.getApiUrl('/api/v1/vehicles');
    return this.http.post<{ id: string }>(url, request);
  }

  /**
   * PUT /api/v1/vehicles/{id}
   * Updates an existing vehicle in inventory
   */
  updateVehicle(id: string, request: UpdateVehicleRequest): Observable<{ data: { id: string } }> {
    const url = this.apiConfig.getApiUrl(`/api/v1/vehicles/${id}`);
    return this.http.put<{ data: { id: string } }>(url, request);
  }

  /**
   * POST /api/v1/vehicles/{id}/mark-sold
   * Transitions vehicle to Sold using optimistic concurrency (rowVersion)
   */
  markVehicleSold(id: string, request: MarkVehicleSoldRequest): Observable<{ data: { id: string } }> {
    const url = this.apiConfig.getApiUrl(`/api/v1/vehicles/${id}/mark-sold`);
    return this.http.post<{ data: { id: string } }>(url, request);
  }

  /**
   * POST /api/v1/vehicles/{id}/transfer
   * Transfers a vehicle to another dealership showroom
   */
  transferDealership(id: string, request: TransferVehicleRequest): Observable<{ data: { id: string } }> {
    const url = this.apiConfig.getApiUrl(`/api/v1/vehicles/${id}/transfer`);
    return this.http.post<{ data: { id: string } }>(url, request);
  }

  /**
   * POST /api/v1/vehicles/{id}/actions
   * Logs a vehicle action (e.g., price reduction, review, transfer)
   */
  logVehicleAction(id: string, request: LogVehicleActionRequest): Observable<{ data: { id: string } }> {
    const url = this.apiConfig.getApiUrl(`/api/v1/vehicles/${id}/actions`);
    return this.http.post<{ data: { id: string } }>(url, request);
  }

  /**
   * GET /api/v1/vehicles/{id}/actions
   * Retrieves paginated log actions recorded on a vehicle
   */
  getVehicleActions(id: string, page = 1, limit = 50): Observable<PagedResponse<VehicleAction>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('limit', limit.toString());

    const url = this.apiConfig.getApiUrl(`/api/v1/vehicles/${id}/actions`);
    return this.http.get<PagedResponse<VehicleAction>>(url, { params });
  }

  /**
   * GET /api/v1/vehicles/aging-stock
   * Lists vehicles on lot longer than threshold
   */
  getAgingStock(page = 1, limit = 20): Observable<PagedResponse<Vehicle>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('limit', limit.toString());

    const url = this.apiConfig.getApiUrl('/api/v1/vehicles/aging-stock');
    return this.http.get<PagedResponse<Vehicle>>(url, { params });
  }
}
