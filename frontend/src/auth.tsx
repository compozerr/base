import { createContext, useContext, ReactNode } from 'react';

export interface AuthContextType {
  isAuthenticated: boolean;
  user: null | { id: string; name?: string };
  login: (credentials: { username: string; password: string }) => Promise<void>;
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

export const useAuth = () => useContext(AuthContext);