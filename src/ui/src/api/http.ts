import { tokenStore } from './tokenStore';

export const BASE_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:7000';

/**
 * A non-2xx API response. `body` is kept for diagnostics only: it is raw, untranslated server
 * text and must never be shown to users. Map it with `errorMessage()` from `@/i18n/errors`.
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

interface ApiRequestInit extends RequestInit {
  /** Skip the bearer token, for the anonymous auth endpoints. */
  anonymous?: boolean;
}

/** JSON request to the API, authenticated unless `anonymous`. Resolves to `undefined` for 204. */
export async function apiRequest<T>(path: string, { anonymous, ...init }: ApiRequestInit = {}): Promise<T> {
  const token = anonymous ? null : tokenStore.get();
  const res = await fetch(`${BASE_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  });
  if (!res.ok) throw new ApiError(res.status, await res.text());
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}
