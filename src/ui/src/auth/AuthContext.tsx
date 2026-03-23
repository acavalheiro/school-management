import { createContext, useContext, useState, type ReactNode } from 'react';
import { tokenStore } from '../api/authApi';

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
