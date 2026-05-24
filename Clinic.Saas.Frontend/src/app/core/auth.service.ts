import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthSession, Role, User } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storageKey = 'clinicflow.session';
  private refreshPromise: Promise<AuthSession> | null = null;
  private refreshTimer: number | null = null;
  readonly session = signal<AuthSession | null>(this.readSession());
  readonly user = computed(() => this.session()?.user ?? null);
  readonly tenant = computed(() => this.session()?.tenant ?? null);
  readonly isAuthenticated = computed(() => Boolean(this.session()?.accessToken));

  constructor() {
    this.scheduleRefresh();
  }

  setSession(session: AuthSession) {
    localStorage.setItem(this.storageKey, JSON.stringify(session));
    this.session.set(session);
    this.scheduleRefresh();
  }

  clear() {
    localStorage.removeItem(this.storageKey);
    this.session.set(null);
    this.clearRefreshTimer();
  }

  token() {
    return this.session()?.accessToken ?? null;
  }

  shouldRefreshToken(bufferMs = 60_000) {
    const expiresAt = this.session()?.expiresAt;
    if (!expiresAt) return false;
    return new Date(expiresAt).getTime() - Date.now() <= bufferMs;
  }

  refreshSession() {
    const refreshToken = this.session()?.refreshToken;
    if (!refreshToken) {
      this.clear();
      return Promise.reject(new Error('لا توجد جلسة صالحة للتجديد'));
    }

    if (!this.refreshPromise) {
      this.refreshPromise = firstValueFrom(
        this.http.post<{ data: AuthSession }>('/api/Auth/refresh', { refreshToken }),
      )
        .then((response) => {
          this.setSession(response.data);
          return response.data;
        })
        .catch((error) => {
          this.clear();
          throw error;
        })
        .finally(() => {
          this.refreshPromise = null;
        });
    }

    return this.refreshPromise;
  }

  private scheduleRefresh() {
    this.clearRefreshTimer();

    const expiresAt = this.session()?.expiresAt;
    if (!expiresAt) return;

    const refreshInMs = Math.max(5_000, new Date(expiresAt).getTime() - Date.now() - 90_000);
    this.refreshTimer = window.setTimeout(() => {
      this.refreshSession().catch(() => this.clear());
    }, refreshInMs);
  }

  private clearRefreshTimer() {
    if (this.refreshTimer) {
      window.clearTimeout(this.refreshTimer);
      this.refreshTimer = null;
    }
  }

  hasRole(roles: Role[]) {
    const role = this.user()?.role;
    return Boolean(role && roles.includes(role));
  }

  private readSession(): AuthSession | null {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthSession;
    } catch {
      return null;
    }
  }
}
