import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import Layout from './Layout';
import { useAuth } from '../auth/AuthContext';
import { useTheme } from './theme-provider';

vi.mock('../auth/AuthContext');
vi.mock('./theme-provider');

const mockedUseAuth = vi.mocked(useAuth);
const mockedUseTheme = vi.mocked(useTheme);

function setup(role: string, theme: 'light' | 'dark' = 'light') {
  const logout = vi.fn();
  const setTheme = vi.fn();
  mockedUseAuth.mockReturnValue({
    user: { email: 'someone@school.test', role, tenantId: 'tenant-1' },
    login: vi.fn(),
    logout,
  });
  mockedUseTheme.mockReturnValue({ theme, setTheme });

  render(
    <MemoryRouter>
      <Layout />
    </MemoryRouter>,
  );

  return { logout, setTheme };
}

describe('Layout', () => {
  it('hides the Tenants link for an Admin', () => {
    setup('Admin');

    expect(screen.getByRole('link', { name: /dashboard/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /students/i })).toBeInTheDocument();
    expect(screen.queryByRole('link', { name: /tenants/i })).not.toBeInTheDocument();
  });

  it('shows the Tenants link for a SuperAdmin', () => {
    setup('SuperAdmin');

    expect(screen.getByRole('link', { name: /tenants/i })).toBeInTheDocument();
  });

  it('calls logout when "Sign out" is clicked', async () => {
    const user = userEvent.setup();
    const { logout } = setup('Admin');

    await user.click(screen.getByRole('button', { name: /sign out/i }));

    expect(logout).toHaveBeenCalledOnce();
  });

  it('calls setTheme with the opposite value when the theme toggle is clicked', async () => {
    const user = userEvent.setup();
    const { setTheme } = setup('Admin', 'light');

    await user.click(screen.getByRole('button', { name: /dark mode/i }));

    expect(setTheme).toHaveBeenCalledWith('dark');
  });

  it("renders the logged-in user's email", () => {
    setup('Admin');

    expect(screen.getByText('someone@school.test')).toBeInTheDocument();
  });
});
