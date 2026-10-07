import { afterEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import TenantDetailPage from './TenantDetailPage';
import { tenantsApi, type TenantDto, type TenantUserDto } from '../api/tenantsApi';

vi.mock('../api/tenantsApi');

const mockedGet = vi.mocked(tenantsApi.get);
const mockedListUsers = vi.mocked(tenantsApi.listUsers);
const mockedCreateUser = vi.mocked(tenantsApi.createUser);

const TENANT_ID = 'tenant-1';

function tenant(overrides: Partial<TenantDto> = {}): TenantDto {
  return { id: TENANT_ID, name: 'Greenfield Academy', createdAt: '2024-01-01T00:00:00Z', ...overrides };
}

function tenantUser(overrides: Partial<TenantUserDto> = {}): TenantUserDto {
  return { id: crypto.randomUUID(), email: 'staff@greenfield.edu', role: 'Admin', ...overrides };
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={[`/tenants/${TENANT_ID}`]}>
      <Routes>
        <Route path="/tenants/:id" element={<TenantDetailPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('TenantDetailPage', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('loads the tenant and its users via Promise.all and renders both', async () => {
    mockedGet.mockResolvedValue(tenant());
    mockedListUsers.mockResolvedValue([tenantUser({ email: 'admin@greenfield.edu', role: 'Admin' })]);

    renderPage();

    expect(await screen.findByText('Greenfield Academy')).toBeInTheDocument();
    expect(screen.getByText('admin@greenfield.edu')).toBeInTheDocument();
  });

  it('shows an empty state when the tenant has no users', async () => {
    mockedGet.mockResolvedValue(tenant());
    mockedListUsers.mockResolvedValue([]);

    renderPage();

    expect(await screen.findByText('No users yet')).toBeInTheDocument();
  });

  it('shows an error state when loading fails', async () => {
    mockedGet.mockResolvedValue(tenant());
    mockedListUsers.mockRejectedValue(new Error('Users service unavailable'));

    renderPage();

    expect(await screen.findByText('Users service unavailable')).toBeInTheDocument();
  });

  it('creates a user and shows the one-time temporary password panel', async () => {
    const user = userEvent.setup();
    mockedGet.mockResolvedValue(tenant());
    mockedListUsers.mockResolvedValue([]);
    mockedCreateUser.mockResolvedValue({
      userId: crypto.randomUUID(),
      email: 'newstaff@greenfield.edu',
      role: 'Admin',
      temporaryPassword: 'Temp!Pass123',
    });

    renderPage();
    await screen.findByText('No users yet');

    await user.click(screen.getAllByRole('button', { name: /add user/i })[0]);
    await user.type(screen.getByLabelText(/email/i), 'newstaff@greenfield.edu');
    await user.click(screen.getByRole('button', { name: /create user/i }));

    expect(await screen.findByText('Temp!Pass123')).toBeInTheDocument();
    expect(screen.getByText('newstaff@greenfield.edu')).toBeInTheDocument();
  });

  it('shows an inline error and keeps the dialog open when creation fails', async () => {
    const user = userEvent.setup();
    mockedGet.mockResolvedValue(tenant());
    mockedListUsers.mockResolvedValue([tenantUser()]);
    mockedCreateUser.mockRejectedValue(new Error('Email already in use'));

    renderPage();
    await screen.findByText('staff@greenfield.edu');

    await user.click(screen.getByRole('button', { name: /add user/i }));
    await user.type(screen.getByLabelText(/email/i), 'dup@greenfield.edu');
    await user.click(screen.getByRole('button', { name: /create user/i }));

    expect(await screen.findByText('Email already in use')).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('copies the temporary password to the clipboard and reverts the icon after a delay', async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });

    mockedGet.mockResolvedValue(tenant());
    mockedListUsers.mockResolvedValue([]);
    mockedCreateUser.mockResolvedValue({
      userId: crypto.randomUUID(),
      email: 'newstaff@greenfield.edu',
      role: 'Admin',
      temporaryPassword: 'Temp!Pass123',
    });

    renderPage();
    await screen.findByText('No users yet');
    await user.click(screen.getAllByRole('button', { name: /add user/i })[0]);
    await user.type(screen.getByLabelText(/email/i), 'newstaff@greenfield.edu');
    await user.click(screen.getByRole('button', { name: /create user/i }));
    await screen.findByText('Temp!Pass123');

    const copyButton = screen.getAllByRole('button').find((b) => b.querySelector('svg.lucide-copy'))!;
    await user.click(copyButton);

    expect(writeText).toHaveBeenCalledWith('Temp!Pass123');
    await waitFor(() => {
      expect(document.querySelector('svg.lucide-check')).toBeInTheDocument();
    });

    await vi.advanceTimersByTimeAsync(2000);

    await waitFor(() => {
      expect(document.querySelector('svg.lucide-copy')).toBeInTheDocument();
    });
  });

  it('resets to the form (not the created panel) when reopening after a successful create', async () => {
    const user = userEvent.setup();
    mockedGet.mockResolvedValue(tenant());
    // Empty for the initial load only; handleCreate's post-create refetch (and any
    // later reload) should see the newly created user, matching real API behavior.
    mockedListUsers.mockResolvedValueOnce([]);
    mockedListUsers.mockResolvedValue([tenantUser({ email: 'newstaff@greenfield.edu' })]);
    mockedCreateUser.mockResolvedValue({
      userId: crypto.randomUUID(),
      email: 'newstaff@greenfield.edu',
      role: 'Admin',
      temporaryPassword: 'Temp!Pass123',
    });

    renderPage();
    await screen.findByText('No users yet');
    await user.click(screen.getAllByRole('button', { name: /add user/i })[0]);
    await user.type(screen.getByLabelText(/email/i), 'newstaff@greenfield.edu');
    await user.click(screen.getByRole('button', { name: /create user/i }));
    await screen.findByText('Temp!Pass123');

    await user.click(screen.getByRole('button', { name: /^done$/i }));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

    // The table (not the empty state) now renders, so there is exactly one "Add User"
    // button — reopening it must show the fresh form, not the stale created panel.
    await user.click(await screen.findByRole('button', { name: /add user/i }));

    expect(screen.getByLabelText(/email/i)).toHaveValue('');
    expect(screen.queryByText('Temp!Pass123')).not.toBeInTheDocument();
  });
});
