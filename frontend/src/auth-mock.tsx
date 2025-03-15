import { createContext, useContext, ReactNode } from 'react';

export type AuthUserType = {
  id: string;
  email: string;
  name: string;
  avatarUrl: string;
};

export interface AuthContextType {
  isAuthenticated: boolean;
  user: AuthUserType | null;
  login: () => Promise<void>;
  logout: () => Promise<void>;
}

const defaultAuth: AuthContextType = {
  isAuthenticated: false,
  user: null,
  login: async () => {
    console.warn('Auth module not implemented - using mock auth');
    return Promise.resolve();
  },
  logout: async () => {
    console.warn('Auth module not implemented - using mock auth');
    return Promise.resolve();
  }
};

const AuthContext = createContext<AuthContextType>(defaultAuth);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  return (
    <AuthContext.Provider value={defaultAuth}>
      {children}
    </AuthContext.Provider>
  );
};

export const useDynamicAuth = () => useContext(AuthContext);