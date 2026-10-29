import axios, { AxiosError, AxiosInstance, InternalAxiosRequestConfig } from 'axios';

const DEFAULT_BASE_URL = 'http://localhost:8080';
const STORAGE_KEY = 'iid_api_base_url';

function getInitialBaseUrl(): string {
  const saved = localStorage.getItem(STORAGE_KEY);
  if (!saved || saved.includes(':5000')) {
    localStorage.setItem(STORAGE_KEY, DEFAULT_BASE_URL);
    return DEFAULT_BASE_URL;
  }
  return saved && saved.trim() !== '' ? saved.trim() : DEFAULT_BASE_URL;
}

class ApiConfigService {
  private _baseUrl = getInitialBaseUrl();

  baseUrl(): string {
    return this._baseUrl;
  }

  setBaseUrl(url: string): void {
    const clean = url.trim().replace(/\/+$/, '');
    localStorage.setItem(STORAGE_KEY, clean);
    this._baseUrl = clean;
  }

  getApiUrl(path: string): string {
    const cleanPath = path.startsWith('/') ? path : `/${path}`;
    return `${this._baseUrl}${cleanPath}`;
  }

  getHubUrl(): string {
    return `${this._baseUrl}/hubs/inventory`;
  }
}

export const apiConfig = new ApiConfigService();

/**
 * HTTP error shape that mirrors Angular's HttpErrorResponse
 * ({ status, statusText, error, message }) so `extractErrorMessage` and the
 * component error handling keep working unchanged.
 */
export class HttpError extends Error {
  readonly status: number;
  readonly statusText: string;
  readonly error: unknown;
  readonly url?: string;
  readonly config?: InternalAxiosRequestConfig;

  constructor(init: {
    status: number;
    statusText: string;
    error: unknown;
    message: string;
    url?: string;
    config?: InternalAxiosRequestConfig;
  }) {
    super(init.message);
    this.name = 'HttpError';
    this.status = init.status;
    this.statusText = init.statusText;
    this.error = init.error;
    this.url = init.url;
    this.config = init.config;
  }
}

export const http: AxiosInstance = axios.create();

// NOTE: this normaliser is registered first, so interceptors added later
// (auth interceptor) receive an `HttpError`.
http.interceptors.response.use(
  response => response,
  (err: AxiosError) => {
    const status = err.response?.status ?? 0;
    const statusText = err.response?.statusText ?? '';
    const url = err.config?.url;
    return Promise.reject(
      new HttpError({
        status,
        statusText,
        error: err.response?.data ?? null,
        message: `Http failure response for ${url ?? '(unknown url)'}: ${status} ${statusText}`.trim(),
        url,
        config: err.config
      })
    );
  }
);

/** GET helper returning only the response body (equivalent of HttpClient.get). */
export async function get<T>(url: string, params?: Record<string, string | number | boolean>): Promise<T> {
  return (await http.get<T>(url, { params })).data;
}

export async function post<T>(url: string, body?: unknown): Promise<T> {
  return (await http.post<T>(url, body)).data;
}

export async function put<T>(url: string, body?: unknown): Promise<T> {
  return (await http.put<T>(url, body)).data;
}

export async function del<T>(url: string): Promise<T> {
  return (await http.delete<T>(url)).data;
}
