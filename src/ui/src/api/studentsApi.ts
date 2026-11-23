import { apiRequest as request } from './http';

export interface StudentDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  dateOfBirth: string;
  // Domain.Enums.StudentStatus by name ("Active", "Inactive", "Graduated", "Suspended").
  // StatusBadge also accepts the numeric value the API sent before enums were serialised as names.
  status: string | number;
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
