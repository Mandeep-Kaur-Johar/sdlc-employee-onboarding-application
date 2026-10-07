import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { TOKEN_STORAGE_KEY } from '../services/apiClient';
import type { AppRole } from '../models/onboarding';

export interface AuthUser {
  email: string;
  displayName: string;
  roles: AppRole[];
}

interface AuthContextValue {
  user: AuthUser | null;
  signIn: (user: AuthUser, token: string) => void;
  signOut: () => void;
  hasRole: (...roles: AppRole[]) => boolean;
}

const USER_STORAGE_KEY = 'onboarding.user';

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredUser(): AuthUser | null {
  const raw = localStorage.getItem(USER_STORAGE_KEY);
  if (!raw) {
    return null;
  }
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => readStoredUser());

  const signIn = useCallback((nextUser: AuthUser, token: string) => {
    localStorage.setItem(TOKEN_STORAGE_KEY, token);
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(nextUser));
    setUser(nextUser);
  }, []);

  const signOut = useCallback(() => {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    localStorage.removeItem(USER_STORAGE_KEY);
    setUser(null);
  }, []);

  const hasRole = useCallback(
    (...roles: AppRole[]) => (user ? roles.some((role) => user.roles.includes(role)) : false),
    [user],
  );

  const value = useMemo<AuthContextValue>(
    () => ({ user, signIn, signOut, hasRole }),
    [user, signIn, signOut, hasRole],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside an AuthProvider.');
  }
  return context;
}
