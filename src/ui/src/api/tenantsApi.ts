import { tokenStore } from './authApi';

const BASE_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:7000';

export interface TenantDto {
  id: string;
  name: string;
  createdAt: string;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = tokenStore.get();
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    ...init,
  });
  if (!res.ok) throw new Error(`API error ${res.status}: ${await res.text()}`);
  return res.json() as Promise<T>;
}

export const tenantsApi = {
  list: () => request<TenantDto[]>('/api/tenants'),
  create: (name: string) =>
    request<{ tenantId: string }>('/api/tenants', {
      method: 'POST',
      body: JSON.stringify({ name }),
    }),
};
