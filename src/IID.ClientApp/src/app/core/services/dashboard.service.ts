import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse, PagedResponse } from '../models/api-response.model';
import { AgingStockItem, DashboardBundle, LowInventoryAlert } from '../models/dashboard.model';
import { ApiConfigService } from './api.service';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly apiConfig = inject(ApiConfigService);

  /**
   * GET /api/v1/dashboard
   * Full dashboard bundle (summary, stats, charts, action center, ai insights, inventory)
   */
  getDashboardBundle(page = 1, pageSize = 20, dealershipId?: string | null): Observable<ApiResponse<DashboardBundle>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (dealershipId) {
      params = params.set('dealershipId', dealershipId);
    }

    const url = this.apiConfig.getApiUrl('/api/v1/dashboard');
    return this.http.get<ApiResponse<DashboardBundle>>(url, { params });
  }

  /**
   * GET /api/v1/dashboard/aging
   * Aging vehicles (> minAgeDays on lot)
   */
  getDashboardAging(minAgeDays = 60, page = 1, limit = 20, dealershipId?: string | null): Observable<PagedResponse<AgingStockItem>> {
    let params = new HttpParams()
      .set('minAgeDays', minAgeDays.toString())
      .set('page', page.toString())
      .set('limit', limit.toString());

    if (dealershipId) {
      params = params.set('dealershipId', dealershipId);
    }

    const url = this.apiConfig.getApiUrl('/api/v1/dashboard/aging');
    return this.http.get<PagedResponse<AgingStockItem>>(url, { params });
  }

  /**
   * GET /api/v1/dashboard/alerts/low-inventory
   * Low inventory make/model threshold alerts
   */
  getLowInventoryAlerts(dealershipId?: string | null): Observable<{ data: readonly LowInventoryAlert[] }> {
    let params = new HttpParams();
    if (dealershipId) {
      params = params.set('dealershipId', dealershipId);
    }
    const url = this.apiConfig.getApiUrl('/api/v1/dashboard/alerts/low-inventory');
    return this.http.get<{ data: readonly LowInventoryAlert[] }>(url, { params });
  }
}
