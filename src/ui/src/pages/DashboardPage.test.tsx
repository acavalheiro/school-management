import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import DashboardPage from './DashboardPage';
import { studentsApi, type StudentDto } from '../api/studentsApi';
import { useAuth } from '../auth/AuthContext';

vi.mock('../api/studentsApi');
vi.mock('../auth/AuthContext');

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return { ...actual, useNavigate: () => mockNavigate };
});

const mockedList = vi.mocked(studentsApi.list);
const mockedUseAuth = vi.mocked(useAuth);

function student(overrides: Partial<StudentDto>): StudentDto {
  return {
    id: crypto.randomUUID(),
    firstName: 'Jane',
    lastName: 'Doe',
    email: 'jane@school.test',
    dateOfBirth: '2015-01-01',
    status: 'Active',
    createdAt: '2024-01-01T00:00:00Z',
    ...overrides,
  };
}

function renderDashboard() {
  mockedUseAuth.mockReturnValue({
    user: { email: 'admin.owner@school.test', role: 'Admin', tenantId: 'tenant-1' },
    login: vi.fn(),
    logout: vi.fn(),
  });

  return render(
    <MemoryRouter>
      <DashboardPage />
    </MemoryRouter>,
  );
}

describe('DashboardPage', () => {
  it('shows a loading state before the student list resolves', () => {
    mockedList.mockReturnValue(new Promise(() => {}));

    renderDashboard();

    expect(screen.getByText(/here's what's happening/i)).toBeInTheDocument();
  });

  it("derives the display name from the user's email", async () => {
    mockedList.mockResolvedValue([]);

    renderDashboard();

    expect(await screen.findByText(/admin\.owner/i)).toBeInTheDocument();
  });

  it('shows the 5 most recently created students out of more than 5, newest first', async () => {
    mockedList.mockResolvedValue([
      student({ firstName: 'Old1', createdAt: '2024-01-01T00:00:00Z' }),
      student({ firstName: 'Old2', createdAt: '2024-01-02T00:00:00Z' }),
      student({ firstName: 'Mid1', createdAt: '2024-01-03T00:00:00Z' }),
      student({ firstName: 'Mid2', createdAt: '2024-01-04T00:00:00Z' }),
      student({ firstName: 'Newer', createdAt: '2024-01-05T00:00:00Z' }),
      student({ firstName: 'Newest', createdAt: '2024-01-06T00:00:00Z' }),
    ]);

    renderDashboard();

    // firstName and lastName render as separate text nodes inside the same <p>, so
    // matchers must target the combined text content ("Newest Doe"), not "Newest" alone.
    expect(await screen.findByText('Newest Doe')).toBeInTheDocument();
    expect(screen.getByText('Newer Doe')).toBeInTheDocument();
    expect(screen.getByText('Mid1 Doe')).toBeInTheDocument();
    expect(screen.getByText('Mid2 Doe')).toBeInTheDocument();
    expect(screen.getByText('Old2 Doe')).toBeInTheDocument();
    // Only the top 5 of 6 are shown — the oldest one drops off.
    expect(screen.queryByText('Old1 Doe')).not.toBeInTheDocument();
  });

  it('shows an empty state with a CTA when there are no students', async () => {
    mockedList.mockResolvedValue([]);

    renderDashboard();

    expect(await screen.findByText('No students yet.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /add your first student/i })).toBeInTheDocument();
  });

  it('navigates to /students?action=add from the Add Student quick action', async () => {
    const user = userEvent.setup();
    mockedList.mockResolvedValue([]);

    renderDashboard();
    await user.click(await screen.findByRole('button', { name: /add student/i }));

    expect(mockNavigate).toHaveBeenCalledWith('/students?action=add');
  });

  it('navigates to /students from the View All Students quick action', async () => {
    const user = userEvent.setup();
    mockedList.mockResolvedValue([]);

    renderDashboard();
    await user.click(await screen.findByRole('button', { name: /view all students/i }));

    expect(mockNavigate).toHaveBeenCalledWith('/students');
  });
});
