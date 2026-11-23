import { tokenStore } from './authApi';

export const BASE_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:7000';

/**
 * A non-2xx API response. `body` is kept for diagnostics only: it is raw server text and must
 * never be shown to users. Map `status` to a translated message instead.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly body: string;

  constructor(status: number, body: string) {
    super(`API error ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.body = body;
  }
}

/** Authenticated JSON request. Resolves to `undefined` for 204 No Content. */
export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const token = tokenStore.get();
  const res = await fetch(`${BASE_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });
  if (!res.ok) throw new ApiError(res.status, await res.text());
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}
