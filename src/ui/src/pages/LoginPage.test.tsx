import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import LoginPage from './LoginPage';
import { authApi } from '../api/authApi';
import i18n from '../i18n';
import { ApiError } from '../api/http';

vi.mock('../api/authApi');

const mockedLogin = vi.mocked(authApi.login);

describe('LoginPage', () => {
  it('calls onLogin(token) on a successful login', async () => {
    const user = userEvent.setup();
    mockedLogin.mockResolvedValue({ token: 'jwt-abc', expiresAt: '2030-01-01T00:00:00Z' });
    const onLogin = vi.fn();

    render(<LoginPage onLogin={onLogin} />);
    await user.type(screen.getByLabelText(/email/i), 'admin@school.test');
    await user.type(screen.getByLabelText(/password/i), 'Password123!');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(mockedLogin).toHaveBeenCalledWith('admin@school.test', 'Password123!');
    expect(onLogin).toHaveBeenCalledWith('jwt-abc');
  });

  it('shows a generic error message on any rejection, not the underlying error', async () => {
    const user = userEvent.setup();
    mockedLogin.mockRejectedValue(new Error('401: some server-internal detail'));

    render(<LoginPage onLogin={vi.fn()} />);
    await user.type(screen.getByLabelText(/email/i), 'admin@school.test');
    await user.type(screen.getByLabelText(/password/i), 'wrong');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByText('Invalid email or password.')).toBeInTheDocument();
    expect(screen.queryByText(/server-internal detail/)).not.toBeInTheDocument();
  });

  it('disables the submit button and shows "Signing in…" while the request is pending', async () => {
    const user = userEvent.setup();
    let resolveLogin!: (value: { token: string; expiresAt: string }) => void;
    mockedLogin.mockReturnValue(
      new Promise((resolve) => {
        resolveLogin = resolve;
      }),
    );

    render(<LoginPage onLogin={vi.fn()} />);
    await user.type(screen.getByLabelText(/email/i), 'admin@school.test');
    await user.type(screen.getByLabelText(/password/i), 'Password123!');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    const pendingButton = await screen.findByRole('button', { name: /signing in/i });
    expect(pendingButton).toBeDisabled();

    resolveLogin({ token: 'jwt-abc', expiresAt: '2030-01-01T00:00:00Z' });

    expect(await screen.findByRole('button', { name: /^sign in$/i })).not.toBeDisabled();
  });

  it('switches to Portuguese from the language picker before signing in', async () => {
    const user = userEvent.setup();

    render(<LoginPage onLogin={vi.fn()} />);
    await user.selectOptions(screen.getByRole('combobox', { name: /language/i }), 'pt-PT');

    expect(screen.getByRole('button', { name: 'Iniciar sessão' })).toBeInTheDocument();
    expect(screen.getByLabelText('Palavra-passe')).toBeInTheDocument();
    expect(document.documentElement.lang).toBe('pt-PT');
  });

  it('shows the invalid-credentials message in the active language', async () => {
    const user = userEvent.setup();
    mockedLogin.mockRejectedValue(new Error('401: Unauthorized'));
    await i18n.changeLanguage('pt-PT');

    render(<LoginPage onLogin={vi.fn()} />);
    await user.type(screen.getByLabelText(/email/i), 'admin@school.test');
    await user.type(screen.getByLabelText('Palavra-passe'), 'wrong');
    await user.click(screen.getByRole('button', { name: 'Iniciar sessão' }));

    expect(await screen.findByText('Email ou palavra-passe inválidos.')).toBeInTheDocument();
  });

  it('says so when rate-limited instead of blaming the credentials', async () => {
    const user = userEvent.setup();
    mockedLogin.mockRejectedValue(new ApiError(429, ''));

    render(<LoginPage onLogin={vi.fn()} />);
    await user.type(screen.getByLabelText(/email/i), 'admin@school.test');
    await user.type(screen.getByLabelText(/password/i), 'Password123!');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByText(/too many attempts/i)).toBeInTheDocument();
  });
});
