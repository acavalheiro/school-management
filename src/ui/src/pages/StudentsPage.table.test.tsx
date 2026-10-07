import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import StudentsPage from './StudentsPage';
import { studentsApi, type StudentDto } from '../api/studentsApi';
import { useAuth } from '../auth/AuthContext';

vi.mock('../api/studentsApi');
vi.mock('../api/tenantsApi');
vi.mock('../auth/AuthContext');

const mockedList = vi.mocked(studentsApi.list);
const mockedUseAuth = vi.mocked(useAuth);

function student(overrides: Partial<StudentDto> = {}): StudentDto {
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

function mockRole(role: string) {
  mockedUseAuth.mockReturnValue({
    user: { email: 'someone@school.test', role, tenantId: 'tenant-1' },
    login: vi.fn(),
    logout: vi.fn(),
  });
}

function renderPage(initialEntry = '/students') {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <Routes>
        <Route path="/students" element={<StudentsPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('StudentsPage table', () => {
  it('shows a populated table once the student list resolves', async () => {
    mockRole('Admin');
    mockedList.mockResolvedValue([student({ firstName: 'Ana', lastName: 'Silva' })]);

    renderPage();

    expect(await screen.findByText('Ana Silva')).toBeInTheDocument();
    expect(screen.getByText('1 student enrolled')).toBeInTheDocument();
  });

  it('shows an empty state with an Add Student CTA for an Admin', async () => {
    mockRole('Admin');
    mockedList.mockResolvedValue([]);

    renderPage();

    expect(await screen.findByText('No students yet')).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /add student/i })).toHaveLength(2);
  });

  it('shows an empty state without an Add Student CTA for a plain User', async () => {
    mockRole('User');
    mockedList.mockResolvedValue([]);

    renderPage();

    expect(await screen.findByText('No students yet')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /add student/i })).not.toBeInTheDocument();
  });

  it('shows the fetch error message instead of the table', async () => {
    mockRole('Admin');
    mockedList.mockRejectedValue(new Error('Network is unreachable'));

    renderPage();

    expect(await screen.findByText('Network is unreachable')).toBeInTheDocument();
  });

  it('shows the "Add Student" header button for Admin and SuperAdmin, hides it for User', async () => {
    mockedList.mockResolvedValue([student()]);

    mockRole('SuperAdmin');
    const { unmount } = renderPage();
    expect(await screen.findByRole('button', { name: /add student/i })).toBeInTheDocument();
    unmount();

    mockRole('User');
    renderPage();
    await screen.findByText('Jane Doe');
    expect(screen.queryByRole('button', { name: /add student/i })).not.toBeInTheDocument();
  });

  it('auto-opens the Add Student dialog when the URL has ?action=add', async () => {
    mockRole('Admin');
    mockedList.mockResolvedValue([]);

    renderPage('/students?action=add');

    expect(await screen.findByRole('dialog')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /add student/i })).toBeInTheDocument();
  });
});
