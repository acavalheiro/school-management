import { apiRequest as request } from './http';

export interface StudentDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  dateOfBirth: string;
  // Domain.Enums.StudentStatus, serialised by the API as its numeric value
  // (1 Active, 2 Inactive, 3 Graduated, 4 Suspended). Strings are tolerated in case that changes.
  status: number | string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateStudentRequest {
  firstName: string;
  lastName: string;
  email: string;
  dateOfBirth: string;
  // Only honoured for a SuperAdmin caller, who must pick the target tenant.
  // Ignored for an Admin, whose tenant comes from their token.
  tenantId?: string;
}


export const studentsApi = {
  list: () => request<StudentDto[]>('/api/students'),
  get: (id: string) => request<StudentDto>(`/api/students/${id}`),
  create: (body: CreateStudentRequest) =>
    request<string>('/api/students', { method: 'POST', body: JSON.stringify(body) }),
};
