import { apiRequest } from './http';

export { tokenStore } from './tokenStore';

export interface AuthTokenDto {
  token: string;
  expiresAt: string;
}

function post<T>(path: string, body: unknown): Promise<T> {
  return apiRequest<T>(path, { method: 'POST', body: JSON.stringify(body), anonymous: true });
}

export const authApi = {
  register: (email: string, password: string, confirmPassword: string) =>
    post<{ userId: string }>('/api/auth/register', { email, password, confirmPassword }),

  login: (email: string, password: string) =>
    post<AuthTokenDto>('/api/auth/login', { email, password }),
};
