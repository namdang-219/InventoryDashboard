import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ApiConfigService {
  private readonly defaultBaseUrl = 'http://localhost:8080';
  private readonly storageKey = 'iid_api_base_url';

  readonly baseUrl = signal<string>(this.getInitialBaseUrl());

  private getInitialBaseUrl(): string {
    const saved = localStorage.getItem(this.storageKey);
    if (!saved || saved.includes(':5000')) {
      localStorage.setItem(this.storageKey, this.defaultBaseUrl);
      return this.defaultBaseUrl;
    }
    return saved && saved.trim() !== '' ? saved.trim() : this.defaultBaseUrl;
  }

  setBaseUrl(url: string): void {
    const clean = url.trim().replace(/\/+$/, '');
    localStorage.setItem(this.storageKey, clean);
    this.baseUrl.set(clean);
  }

  getApiUrl(path: string): string {
    const cleanPath = path.startsWith('/') ? path : `/${path}`;
    return `${this.baseUrl()}${cleanPath}`;
  }

  getHubUrl(): string {
    return `${this.baseUrl()}/hubs/inventory`;
  }
}
