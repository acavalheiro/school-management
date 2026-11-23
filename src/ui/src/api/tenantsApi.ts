import { apiRequest as request } from './http';

export interface TenantDto {
  id: string;
  name: string;
  createdAt: string;
}

export interface TenantUserDto {
  id: string;
  email: string;
  role: string;
}

export interface CreatedUserResponse {
  userId: string;
  email: string;
  role: string;
  temporaryPassword: string;
}


export const tenantsApi = {
  list: () => request<TenantDto[]>('/api/tenants'),
  get: (id: string) => request<TenantDto>(`/api/tenants/${id}`),
  create: (name: string) =>
    request<{ tenantId: string }>('/api/tenants', {
      method: 'POST',
      body: JSON.stringify({ name }),
    }),
  listUsers: (tenantId: string) =>
    request<TenantUserDto[]>(`/api/tenants/${tenantId}/users`),
  createUser: (tenantId: string, body: { email: string; role: string }) =>
    request<CreatedUserResponse>(`/api/tenants/${tenantId}/users`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),
};
