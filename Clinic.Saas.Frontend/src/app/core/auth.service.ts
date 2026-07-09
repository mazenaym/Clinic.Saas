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
  private sessionVersion = 0;
  readonly session = signal<AuthSession | null>(this.readSession());
  readonly user = computed(() => this.session()?.user ?? null);
  readonly tenant = computed(() => this.session()?.tenant ?? null);
  readonly isAuthenticated = computed(() => this.hasValidSession(this.session()));

  constructor() {
    this.scheduleRefresh();
  }

  setSession(session: AuthSession) {
    this.sessionVersion++;
    localStorage.setItem(this.storageKey, JSON.stringify(session));
    this.session.set(session);
    this.scheduleRefresh();
  }

  clear() {
    this.sessionVersion++;
    this.refreshPromise = null;
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
      const version = this.sessionVersion;
      this.refreshPromise = firstValueFrom(
        this.http.post<{ data: AuthSession }>('/api/Auth/refresh', { refreshToken }),
      )
        .then((response) => {
          if (version !== this.sessionVersion) {
            throw new Error('Session changed during refresh.');
          }
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
    return this.isAuthenticated() && Boolean(role && roles.includes(role));
  }

  private hasValidSession(session: AuthSession | null) {
    if (!session?.accessToken || !session.user) return false;
    if (!session.expiresAt) return true;
    return new Date(session.expiresAt).getTime() > Date.now();
  }

  private readSession(): AuthSession | null {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) return null;
    try {
      const session = JSON.parse(raw) as AuthSession;
      if (!this.hasValidSession(session)) {
        localStorage.removeItem(this.storageKey);
        return null;
      }
      return session;
    } catch {
      localStorage.removeItem(this.storageKey);
      return null;
    }
  }
}
