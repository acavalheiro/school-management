import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import StudentsPage from './StudentsPage';
import DashboardPage from './DashboardPage';
import { studentsApi, type StudentDto } from '../api/studentsApi';
import { useAuth } from '../auth/AuthContext';

vi.mock('../api/studentsApi');
vi.mock('../api/tenantsApi');
vi.mock('../auth/AuthContext');

// The shape GET /api/students returns from a running API. StudentStatus used to arrive as a number,
// which crashed both pages ("status.toLowerCase is not a function"); it now arrives by name, and
// StatusBadge still copes with the numeric form (see StatusBadge.test.tsx).
const fromApi: StudentDto = {
  id: '8a3ca62f-de17-4ea8-a489-027aeab9e8f2',
  firstName: 'Ana',
  lastName: 'Teste',
  email: 'ana.teste@example.test',
  dateOfBirth: '2016-11-10',
  status: 'Active',
  createdAt: '2026-11-23T14:01:12.785054Z',
};

function setup() {
  vi.mocked(useAuth).mockReturnValue({
    user: { email: 'admin@school.test', role: 'Admin', tenantId: 'tenant-1' },
    login: vi.fn(),
    logout: vi.fn(),
  });
  vi.mocked(studentsApi.list).mockResolvedValue([fromApi]);
}

describe('student lists with the real API payload', () => {
  it('renders the Students table', async () => {
    setup();
    render(<MemoryRouter><StudentsPage /></MemoryRouter>);

    expect(await screen.findByText('Ana Teste')).toBeInTheDocument();
    expect(screen.getByText('Active')).toBeInTheDocument();
  });

  it('renders the Dashboard recent list', async () => {
    setup();
    render(<MemoryRouter><DashboardPage /></MemoryRouter>);

    expect(await screen.findByText('Ana Teste')).toBeInTheDocument();
    expect(screen.getByText('Active')).toBeInTheDocument();
  });
});
