import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { tokenStore } from '../api/authApi';
import { meApi } from '../api/meApi';
import i18n from '../i18n';
import { isSupportedLanguage } from '../i18n/languages';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

export interface AuthUser {
  email: string;
  role: string;
  tenantId: string;
}

function decodeUser(token: string): AuthUser | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return {
      email: payload.email ?? '',
      role: payload[ROLE_CLAIM] ?? payload.role ?? '',
      tenantId: payload.tid ?? '',
    };
  } catch {
    return null;
  }
}

interface AuthContextValue {
  user: AuthUser | null;
  login: (token: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const token = tokenStore.get();
    return token ? decodeUser(token) : null;
  });

  // Once signed in (on login, or on load with a stored token), the account's saved language wins
  // over the locally cached or browser-detected one, so the choice follows the user across devices.
  const signedInAs = user?.email;
  useEffect(() => {
    if (!signedInAs) return;
    let cancelled = false;
    const languageAtRequest = i18n.language;

    meApi
      .get()
      .then((me) => {
        // Don't overwrite a choice the user made while this request was in flight.
        if (cancelled || i18n.language !== languageAtRequest) return;
        if (isSupportedLanguage(me.preferredLanguage)) void i18n.changeLanguage(me.preferredLanguage);
      })
      .catch(() => {
        // Keep the local language; an expired token is handled by the pages' own requests.
      });

    return () => {
      cancelled = true;
    };
  }, [signedInAs]);

  function login(token: string) {
    tokenStore.set(token);
    setUser(decodeUser(token));
  }

  function logout() {
    tokenStore.clear();
    setUser(null);
  }

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}
