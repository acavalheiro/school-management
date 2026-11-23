import { beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuthProvider, useAuth } from '../auth/AuthContext';
import { LanguageSwitcher } from '../components/LanguageSwitcher';
import { meApi } from '../api/meApi';
import i18n from './index';

vi.mock('../api/meApi');

const mockedGet = vi.mocked(meApi.get);
const mockedUpdate = vi.mocked(meApi.updateLanguage);

const TOKEN = `header.${btoa(JSON.stringify({ email: 'me@school.test', tid: 't1' }))}.signature`;

function me(preferredLanguage: string) {
  return { id: 'u1', email: 'me@school.test', role: 'Admin', preferredLanguage };
}

function LoginButton() {
  const { login } = useAuth();
  return <button onClick={() => login(TOKEN)}>login</button>;
}

function renderApp() {
  return render(
    <AuthProvider>
      <LoginButton />
      <LanguageSwitcher />
    </AuthProvider>,
  );
}

describe('language sync with the account', () => {
  beforeEach(() => {
    mockedGet.mockResolvedValue(me('en-GB'));
    mockedUpdate.mockResolvedValue(undefined);
  });

  it('applies the saved account language on load, over the locally cached one', async () => {
    localStorage.setItem('atl_token', TOKEN);
    await i18n.changeLanguage('en-GB'); // also caches en-GB locally
    mockedGet.mockResolvedValue(me('pt-PT'));

    renderApp();

    await waitFor(() => expect(i18n.language).toBe('pt-PT'));
    expect(localStorage.getItem('atl_lang')).toBe('pt-PT');
  });

  it('applies the saved account language after signing in', async () => {
    const user = userEvent.setup();
    mockedGet.mockResolvedValue(me('pt-PT'));

    renderApp();
    expect(mockedGet).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: 'login' }));

    await waitFor(() => expect(i18n.language).toBe('pt-PT'));
  });

  it('ignores an unsupported language from the server', async () => {
    localStorage.setItem('atl_token', TOKEN);
    mockedGet.mockResolvedValue(me('pt-BR'));

    renderApp();

    await waitFor(() => expect(mockedGet).toHaveBeenCalled());
    expect(i18n.language).toBe('en-GB');
  });

  it('does not call the API when switching language while signed out', async () => {
    const user = userEvent.setup();

    renderApp();
    await user.selectOptions(screen.getByRole('combobox', { name: /language/i }), 'pt-PT');

    expect(i18n.language).toBe('pt-PT');
    expect(mockedUpdate).not.toHaveBeenCalled();
    expect(mockedGet).not.toHaveBeenCalled();
  });

  it('saves the choice to the account when signed in', async () => {
    const user = userEvent.setup();
    localStorage.setItem('atl_token', TOKEN);

    renderApp();
    await waitFor(() => expect(mockedGet).toHaveBeenCalled());
    await user.selectOptions(screen.getByRole('combobox', { name: /language/i }), 'pt-PT');

    await waitFor(() => expect(mockedUpdate).toHaveBeenCalledWith('pt-PT'));
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('keeps the local choice and warns, in the new language, when saving fails', async () => {
    const user = userEvent.setup();
    localStorage.setItem('atl_token', TOKEN);
    mockedUpdate.mockRejectedValue(new Error('network down'));

    renderApp();
    await waitFor(() => expect(mockedGet).toHaveBeenCalled());
    await user.selectOptions(screen.getByRole('combobox', { name: /language/i }), 'pt-PT');

    expect(await screen.findByRole('alert')).toHaveTextContent('Não foi possível guardar o idioma');
    expect(i18n.language).toBe('pt-PT');
  });
});
