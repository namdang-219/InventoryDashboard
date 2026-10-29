export interface ApiResponse<T> {
  data: T;
  meta?: {
    page?: number;
    limit?: number;
    total?: number;
    totalInventory?: number;
    [key: string]: unknown;
  };
}

export interface PagedResponse<T> {
  data: T[];
  meta: {
    page: number;
    limit: number;
    total: number;
  };
}

export interface ApiErrorResponse {
  statusCode?: number;
  message?: string;
  errors?: Record<string, string[]>;
}
