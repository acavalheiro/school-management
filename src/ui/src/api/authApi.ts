const BASE_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:7000';

export interface AuthTokenDto {
  token: string;
  expiresAt: string;
}

async function request<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!res.ok) throw new Error(`${res.status}: ${await res.text()}`);
  return res.json() as Promise<T>;
}

export const authApi = {
  register: (email: string, password: string, confirmPassword: string) =>
    request<{ userId: string }>('/api/auth/register', { email, password, confirmPassword }),

  login: (email: string, password: string) =>
    request<AuthTokenDto>('/api/auth/login', { email, password }),
};

export const tokenStore = {
  get: () => localStorage.getItem('atl_token'),
  set: (token: string) => localStorage.setItem('atl_token', token),
  clear: () => localStorage.removeItem('atl_token'),
};
