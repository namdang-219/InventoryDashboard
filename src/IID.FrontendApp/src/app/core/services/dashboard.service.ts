import { ApiResponse, PagedResponse } from '../models/api-response.model';
import { AgingStockItem, DashboardBundle, LowInventoryAlert } from '../models/dashboard.model';
import { apiConfig, get } from './api.service';

export const dashboardService = {
  /**
   * GET /api/v1/dashboard
   * Full dashboard bundle (summary, stats, charts, action center, ai insights, inventory)
   */
  getDashboardBundle(page = 1, pageSize = 20, dealershipId?: string | null): Promise<ApiResponse<DashboardBundle>> {
    const params: Record<string, string> = { page: page.toString(), pageSize: pageSize.toString() };
    if (dealershipId) params['dealershipId'] = dealershipId;

    return get<ApiResponse<DashboardBundle>>(apiConfig.getApiUrl('/api/v1/dashboard'), params);
  },

  /**
   * GET /api/v1/dashboard/aging
   * Aging vehicles (> minAgeDays on lot)
   */
  getDashboardAging(
    minAgeDays = 60,
    page = 1,
    limit = 20,
    dealershipId?: string | null
  ): Promise<PagedResponse<AgingStockItem>> {
    const params: Record<string, string> = {
      minAgeDays: minAgeDays.toString(),
      page: page.toString(),
      limit: limit.toString()
    };
    if (dealershipId) params['dealershipId'] = dealershipId;

    return get<PagedResponse<AgingStockItem>>(apiConfig.getApiUrl('/api/v1/dashboard/aging'), params);
  },

  /**
   * GET /api/v1/dashboard/alerts/low-inventory
   * Low inventory make/model threshold alerts
   */
  getLowInventoryAlerts(dealershipId?: string | null): Promise<{ data: readonly LowInventoryAlert[] }> {
    const params: Record<string, string> = {};
    if (dealershipId) params['dealershipId'] = dealershipId;

    return get<{ data: readonly LowInventoryAlert[] }>(
      apiConfig.getApiUrl('/api/v1/dashboard/alerts/low-inventory'),
      params
    );
  }
};
