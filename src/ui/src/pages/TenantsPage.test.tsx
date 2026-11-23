import { describe, expect, it, vi } from 'vitest';
import { ApiError } from '../api/http';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import TenantsPage from './TenantsPage';
import { tenantsApi, type TenantDto } from '../api/tenantsApi';
import i18n from '../i18n';

vi.mock('../api/tenantsApi');

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return { ...actual, useNavigate: () => mockNavigate };
});

const mockedList = vi.mocked(tenantsApi.list);
const mockedCreate = vi.mocked(tenantsApi.create);

function tenant(overrides: Partial<TenantDto>): TenantDto {
  return { id: crypto.randomUUID(), name: 'Greenfield Academy', createdAt: '2024-01-01T00:00:00Z', ...overrides };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <TenantsPage />
    </MemoryRouter>,
  );
}

describe('TenantsPage', () => {
  it('shows a loading state before the tenant list resolves, then the table', async () => {
    mockedList.mockResolvedValue([tenant({ name: 'Greenfield Academy' })]);

    renderPage();

    expect(await screen.findByText('Greenfield Academy')).toBeInTheDocument();
  });

  it('shows an empty state when there are no tenants', async () => {
    mockedList.mockResolvedValue([]);

    renderPage();

    expect(await screen.findByText('No tenants yet')).toBeInTheDocument();
  });

  it("renders the error message when the list fails to load", async () => {
    mockedList.mockRejectedValue(new TypeError('Failed to fetch'));

    renderPage();

    expect(await screen.findByText(/couldn't reach the server/i)).toBeInTheDocument();
  });

  it('creates a tenant with the trimmed name, closes the dialog, and reloads the list', async () => {
    const user = userEvent.setup();
    mockedList.mockResolvedValueOnce([tenant({ name: 'Existing Academy' })]);
    mockedCreate.mockResolvedValue({ tenantId: crypto.randomUUID() });

    renderPage();
    await screen.findByText('Existing Academy');

    mockedList.mockResolvedValueOnce([
      tenant({ name: 'Existing Academy' }),
      tenant({ name: 'New Academy' }),
    ]);

    await user.click(screen.getByRole('button', { name: /new tenant/i }));
    await user.type(screen.getByLabelText(/school name/i), '  New Academy  ');
    await user.click(screen.getByRole('button', { name: /create tenant/i }));

    expect(mockedCreate).toHaveBeenCalledWith('New Academy');
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
    expect(await screen.findByText('New Academy')).toBeInTheDocument();
  });

  it('shows an inline error and keeps the dialog open when creation fails', async () => {
    const user = userEvent.setup();
    mockedList.mockResolvedValue([tenant({ name: 'Existing Academy' })]);
    mockedCreate.mockRejectedValue(new ApiError(400, 'Name: Tenant name must not exceed 200 characters.'));

    renderPage();
    await screen.findByText('Existing Academy');

    await user.click(screen.getByRole('button', { name: /new tenant/i }));
    await user.type(screen.getByLabelText(/school name/i), 'Duplicate Academy');
    await user.click(screen.getByRole('button', { name: /create tenant/i }));

    expect(await screen.findByText(/some of the details are not valid/i)).toBeInTheDocument();
    expect(screen.queryByText(/must not exceed/)).not.toBeInTheDocument();
    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('navigates to the tenant detail page when a row is clicked', async () => {
    const user = userEvent.setup();
    const t = tenant({ name: 'Greenfield Academy' });
    mockedList.mockResolvedValue([t]);

    renderPage();
    await user.click(await screen.findByText('Greenfield Academy'));

    expect(mockNavigate).toHaveBeenCalledWith(`/tenants/${t.id}`);
  });

  it('renders in pt-PT with Escola terminology and pt-PT dates', async () => {
    await i18n.changeLanguage('pt-PT');
    mockedList.mockResolvedValue([tenant({ name: 'Escola A', createdAt: '2024-03-05T10:00:00Z' })]);

    renderPage();

    expect(await screen.findByText('Escola A')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Escolas' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /nova escola/i })).toBeInTheDocument();
    expect(screen.getByText('1 escola')).toBeInTheDocument();
    expect(
      screen.getByText(new Intl.DateTimeFormat('pt-PT', { dateStyle: 'medium' }).format(new Date('2024-03-05T10:00:00Z'))),
    ).toBeInTheDocument();
  });
});
