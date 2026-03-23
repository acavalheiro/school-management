import { tokenStore } from './authApi';

const BASE_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:7000';

export interface StudentDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  dateOfBirth: string;
  status: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateStudentRequest {
  firstName: string;
  lastName: string;
  email: string;
  dateOfBirth: string;
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

export const studentsApi = {
  list: () => request<StudentDto[]>('/api/students'),
  get: (id: string) => request<StudentDto>(`/api/students/${id}`),
  create: (body: CreateStudentRequest) =>
    request<string>('/api/students', { method: 'POST', body: JSON.stringify(body) }),
};
