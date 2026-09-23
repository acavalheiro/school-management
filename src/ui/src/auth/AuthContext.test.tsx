import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuthProvider, useAuth } from './AuthContext';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

function makeToken(payload: Record<string, unknown>): string {
  return `header.${btoa(JSON.stringify(payload))}.signature`;
}

function Consumer() {
  const { user, login, logout } = useAuth();
  return (
    <div>
      <div data-testid="user">{user ? JSON.stringify(user) : 'no-user'}</div>
      <button onClick={() => login(makeToken({ email: 'admin@school.test', [ROLE_CLAIM]: 'Admin', tid: 'tenant-1' }))}>
        login
      </button>
      <button onClick={logout}>logout</button>
    </div>
  );
}

describe('AuthContext', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('decodes email, role (namespaced claim), and tenantId from a well-formed token', () => {
    const token = makeToken({ email: 'owner@school.test', [ROLE_CLAIM]: 'SuperAdmin', tid: 'tenant-42' });
    localStorage.setItem('atl_token', token);

    render(
      <AuthProvider>
        <Consumer />
      </AuthProvider>,
    );

    const user = JSON.parse(screen.getByTestId('user').textContent!);
    expect(user).toEqual({ email: 'owner@school.test', role: 'SuperAdmin', tenantId: 'tenant-42' });
  });

  it('falls back to payload.role when the namespaced claim is absent', () => {
    const token = makeToken({ email: 'fallback@school.test', role: 'User', tid: 'tenant-1' });
    localStorage.setItem('atl_token', token);

    render(
      <AuthProvider>
        <Consumer />
      </AuthProvider>,
    );

    const user = JSON.parse(screen.getByTestId('user').textContent!);
    expect(user.role).toBe('User');
  });

  it('starts with no user, without throwing, when the stored token is malformed', () => {
    localStorage.setItem('atl_token', 'not-a-jwt');

    render(
      <AuthProvider>
        <Consumer />
      </AuthProvider>,
    );

    expect(screen.getByTestId('user')).toHaveTextContent('no-user');
  });

  it('starts with no user when no token is stored', () => {
    render(
      <AuthProvider>
        <Consumer />
      </AuthProvider>,
    );

    expect(screen.getByTestId('user')).toHaveTextContent('no-user');
  });

  it('login() stores the token and decodes the user', async () => {
    const user = userEvent.setup();
    render(
      <AuthProvider>
        <Consumer />
      </AuthProvider>,
    );

    await user.click(screen.getByRole('button', { name: 'login' }));

    expect(localStorage.getItem('atl_token')).not.toBeNull();
    expect(screen.getByTestId('user')).toHaveTextContent('admin@school.test');
  });

  it('logout() clears the token and nulls the user', async () => {
    const user = userEvent.setup();
    render(
      <AuthProvider>
        <Consumer />
      </AuthProvider>,
    );
    await user.click(screen.getByRole('button', { name: 'login' }));

    await user.click(screen.getByRole('button', { name: 'logout' }));

    expect(localStorage.getItem('atl_token')).toBeNull();
    expect(screen.getByTestId('user')).toHaveTextContent('no-user');
  });

  it('useAuth() throws when used outside an AuthProvider', () => {
    // Swallow React's expected console.error noise for the thrown-during-render case.
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    expect(() => render(<Consumer />)).toThrow('useAuth must be used inside AuthProvider');

    consoleError.mockRestore();
  });
});
