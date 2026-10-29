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
import { apiConfig, del, get, post, put } from './api.service';

type Params = Record<string, string | number | boolean>;

export const inventoryService = {
  /**
   * GET /api/v1/vehicles
   * Paginated, filterable, sortable vehicle listing
   */
  listVehicles(params: VehicleFilterParams = {}): Promise<PagedResponse<Vehicle>> {
    const query: Params = {};

    if (params.dealershipId) query['dealershipId'] = params.dealershipId;
    if (params.make) query['make'] = params.make;
    if (params.model) query['model'] = params.model;
    if (params.vin) query['vin'] = params.vin;
    if (params.stockNumber) query['stockNumber'] = params.stockNumber;
    if (params.minAgeDays !== undefined && params.minAgeDays !== null) {
      query['minAgeDays'] = params.minAgeDays.toString();
    }
    if (params.maxAgeDays !== undefined && params.maxAgeDays !== null) {
      query['maxAgeDays'] = params.maxAgeDays.toString();
    }
    if (params.status) query['status'] = params.status;
    if (params.page) query['page'] = params.page.toString();
    if (params.limit) query['limit'] = params.limit.toString();
    if (params.sort) query['sort'] = params.sort;
    if (params.order) query['order'] = params.order;

    return get<PagedResponse<Vehicle>>(apiConfig.getApiUrl('/api/v1/vehicles'), query);
  },

  /**
   * GET /api/v1/vehicles/{id}
   * Retrieves single vehicle details
   */
  getVehicleById(id: string): Promise<{ data: Vehicle }> {
    return get<{ data: Vehicle }>(apiConfig.getApiUrl(`/api/v1/vehicles/${id}`));
  },

  /**
   * POST /api/v1/vehicles
   * Creates a new vehicle in inventory
   */
  createVehicle(request: CreateVehicleRequest): Promise<{ id: string }> {
    return post<{ id: string }>(apiConfig.getApiUrl('/api/v1/vehicles'), request);
  },

  /**
   * PUT /api/v1/vehicles/{id}
   * Updates an existing vehicle in inventory
   */
  updateVehicle(id: string, request: UpdateVehicleRequest): Promise<{ data: { id: string } }> {
    return put<{ data: { id: string } }>(apiConfig.getApiUrl(`/api/v1/vehicles/${id}`), request);
  },

  /**
   * POST /api/v1/vehicles/{id}/mark-sold
   * Transitions vehicle to Sold using optimistic concurrency (rowVersion)
   */
  markVehicleSold(id: string, request: MarkVehicleSoldRequest): Promise<{ data: { id: string } }> {
    return post<{ data: { id: string } }>(apiConfig.getApiUrl(`/api/v1/vehicles/${id}/mark-sold`), request);
  },

  /**
   * POST /api/v1/vehicles/{id}/transfer
   * Transfers a vehicle to another dealership showroom
   */
  transferDealership(id: string, request: TransferVehicleRequest): Promise<{ data: { id: string } }> {
    return post<{ data: { id: string } }>(apiConfig.getApiUrl(`/api/v1/vehicles/${id}/transfer`), request);
  },

  /**
   * POST /api/v1/vehicles/{id}/actions
   * Logs a vehicle action (e.g., price reduction, review, transfer)
   */
  logVehicleAction(id: string, request: LogVehicleActionRequest): Promise<{ data: { id: string } }> {
    return post<{ data: { id: string } }>(apiConfig.getApiUrl(`/api/v1/vehicles/${id}/actions`), request);
  },

  /**
   * GET /api/v1/vehicles/{id}/actions
   * Retrieves paginated log actions recorded on a vehicle
   */
  getVehicleActions(id: string, page = 1, limit = 50): Promise<PagedResponse<VehicleAction>> {
    return get<PagedResponse<VehicleAction>>(apiConfig.getApiUrl(`/api/v1/vehicles/${id}/actions`), {
      page: page.toString(),
      limit: limit.toString()
    });
  },

  /**
   * GET /api/v1/vehicle-actions
   * Retrieves all vehicle actions across inventory with filters
   */
  getAllVehicleActions(params?: {
    vehicleId?: string;
    actionType?: string;
    search?: string;
    page?: number;
    limit?: number;
  }): Promise<PagedResponse<VehicleAction>> {
    const query: Params = {};
    if (params?.vehicleId) query['vehicleId'] = params.vehicleId;
    if (params?.actionType) query['actionType'] = params.actionType;
    if (params?.search) query['search'] = params.search;
    if (params?.page) query['page'] = params.page.toString();
    if (params?.limit) query['limit'] = params.limit.toString();

    return get<PagedResponse<VehicleAction>>(apiConfig.getApiUrl('/api/v1/vehicle-actions'), query);
  },

  /**
   * POST /api/v1/vehicle-actions
   * Creates a new action for a vehicle
   */
  createVehicleAction(request: {
    vehicleId: string;
    actionType: string;
    notes?: string | null;
  }): Promise<{ data: { id: string } }> {
    return post<{ data: { id: string } }>(apiConfig.getApiUrl('/api/v1/vehicle-actions'), request);
  },

  /**
   * PUT /api/v1/vehicle-actions/{id}
   * Updates an existing action
   */
  updateVehicleAction(
    id: string,
    request: { actionType: string; notes?: string | null }
  ): Promise<{ data: { id: string } }> {
    return put<{ data: { id: string } }>(apiConfig.getApiUrl(`/api/v1/vehicle-actions/${id}`), request);
  },

  /**
   * DELETE /api/v1/vehicle-actions/{id}
   * Soft deletes a vehicle action
   */
  deleteVehicleAction(id: string): Promise<{ data: { id: string } }> {
    return del<{ data: { id: string } }>(apiConfig.getApiUrl(`/api/v1/vehicle-actions/${id}`));
  },

  /**
   * GET /api/v1/vehicles/aging-stock
   * Lists vehicles on lot longer than threshold
   */
  getAgingStock(page = 1, limit = 20, dealershipId?: string | null): Promise<PagedResponse<Vehicle>> {
    const query: Params = { page: page.toString(), limit: limit.toString() };
    if (dealershipId) query['dealershipId'] = dealershipId;

    return get<PagedResponse<Vehicle>>(apiConfig.getApiUrl('/api/v1/vehicles/aging-stock'), query);
  }
};
