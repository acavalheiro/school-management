import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import StudentsPage from './StudentsPage';
import { studentsApi } from '../api/studentsApi';
import { tenantsApi, type TenantDto } from '../api/tenantsApi';
import { useAuth } from '../auth/AuthContext';

vi.mock('../api/studentsApi');
vi.mock('../api/tenantsApi');
vi.mock('../auth/AuthContext');

const mockedStudentsList = vi.mocked(studentsApi.list);
const mockedCreate = vi.mocked(studentsApi.create);
const mockedTenantsList = vi.mocked(tenantsApi.list);
const mockedUseAuth = vi.mocked(useAuth);

// Same fixed-clock reasoning as StudentsPage.createDialog.test.tsx: the calendar
// disables any date after "today", so the target date must be in the same month and
// safely before it.
const FIXED_NOW = new Date(2024, 6, 20); // July 20, 2024
const TARGET_DATE = new Date(2024, 6, 5); // July 5, 2024

function tenant(overrides: Partial<TenantDto> = {}): TenantDto {
  return { id: crypto.randomUUID(), name: 'Greenfield Academy', createdAt: '2024-01-01T00:00:00Z', ...overrides };
}

function renderPage() {
  mockedUseAuth.mockReturnValue({
    user: { email: 'superadmin@atl.com', role: 'SuperAdmin', tenantId: '' },
    login: vi.fn(),
    logout: vi.fn(),
  });
  mockedStudentsList.mockResolvedValue([]);

  return render(
    <MemoryRouter>
      <Routes>
        <Route path="/" element={<StudentsPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

async function openCreateDialog(user: ReturnType<typeof userEvent.setup>) {
  await screen.findByText('No students yet');
  await user.click(screen.getAllByRole('button', { name: /add student/i })[0]);
  await screen.findByRole('dialog');
}

async function selectTenant(user: ReturnType<typeof userEvent.setup>, name: string) {
  const input = screen.getByLabelText(/tenant/i);
  await user.click(input);
  const option = await screen.findByRole('option', { name });
  await user.click(option);
}

async function pickDate(user: ReturnType<typeof userEvent.setup>, date: Date) {
  await user.click(screen.getByRole('button', { name: /date of birth/i }));
  const dayButton = await waitFor(() => {
    const el = document.querySelector<HTMLElement>(`[data-day="${date.toLocaleDateString()}"]`);
    expect(el).not.toBeNull();
    return el!;
  });
  await user.click(dayButton);
}

async function fillRequiredFieldsExceptTenant(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText(/first name/i), 'Ana');
  await user.type(screen.getByLabelText(/last name/i), 'Silva');
  await user.type(screen.getByLabelText(/email/i), 'ana@school.test');
  await pickDate(user, TARGET_DATE);
}

describe('StudentsPage create dialog (SuperAdmin tenant picker)', () => {
  beforeEach(() => {
    vi.setSystemTime(FIXED_NOW);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('does not fetch tenants until the dialog is opened', async () => {
    mockedTenantsList.mockResolvedValue([tenant()]);
    renderPage();
    await screen.findByText('No students yet');

    expect(mockedTenantsList).not.toHaveBeenCalled();

    const user = userEvent.setup();
    await openCreateDialog(user);

    await waitFor(() => expect(mockedTenantsList).toHaveBeenCalledOnce());
  });

  it('keeps submit disabled until a tenant is selected, even with the rest of the form filled', async () => {
    const user = userEvent.setup();
    mockedTenantsList.mockResolvedValue([tenant({ name: 'Greenfield Academy' })]);
    renderPage();
    await openCreateDialog(user);

    await fillRequiredFieldsExceptTenant(user);
    expect(screen.getByRole('button', { name: /^add student$/i })).toBeDisabled();

    await selectTenant(user, 'Greenfield Academy');
    expect(screen.getByRole('button', { name: /^add student$/i })).not.toBeDisabled();
  });

  it('submits with the selected tenant id', async () => {
    const user = userEvent.setup();
    const targetTenant = tenant({ name: 'Greenfield Academy' });
    mockedTenantsList.mockResolvedValue([targetTenant, tenant({ name: 'Riverside School' })]);
    mockedCreate.mockResolvedValue('new-student-id');
    renderPage();
    await openCreateDialog(user);

    await fillRequiredFieldsExceptTenant(user);
    await selectTenant(user, 'Greenfield Academy');

    await user.click(screen.getByRole('button', { name: /^add student$/i }));

    await waitFor(() => {
      expect(mockedCreate).toHaveBeenCalledWith(
        expect.objectContaining({ tenantId: targetTenant.id }),
      );
    });
  });

  it('resets the selected tenant when the dialog is reopened', async () => {
    const user = userEvent.setup();
    mockedTenantsList.mockResolvedValue([tenant({ name: 'Greenfield Academy' })]);
    renderPage();
    await openCreateDialog(user);
    await selectTenant(user, 'Greenfield Academy');

    await user.click(screen.getByRole('button', { name: /cancel/i }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

    await user.click(screen.getAllByRole('button', { name: /add student/i })[0]);
    await screen.findByRole('dialog');

    // Tenants state already has data from the first open, so the lazy-load effect
    // (guarded on tenants.length === 0) does not refetch on reopen.
    expect(mockedTenantsList).toHaveBeenCalledOnce();
    expect(screen.getByLabelText(/tenant/i)).toHaveValue('');
  });
});
