import * as React from 'react'
import { AuthApi } from './generated';

export type AuthUser = {
    id: string;
    email: string;
    name: string;
    avatarUrl: string;
}

export interface AuthContext {
    isAuthenticated: boolean
    login: () => Promise<void>
    logout: () => Promise<void>
    user: AuthUser | null
}

const AuthContext = React.createContext<AuthContext | null>(null)

const key = 'tanstack.auth.user.id'

async function fetchAndSetUser(setUser: React.Dispatch<React.SetStateAction<AuthUser | null>>) {
    const response = await AuthApi.getV1authme();

    const user: AuthUser = {
        id: response.id!,
        email: response.email!,
        name: response.name!,
        avatarUrl: response.avatarUrl!,
    }

    setUser(user)
    setStoredUser(user)
}

export function getStoredUser(): AuthUser | null {
    const storedUser = localStorage.getItem(key)
    if (storedUser) {
        return JSON.parse(storedUser)
    }
    return null
}

function setStoredUser(user: AuthUser | null) {
    if (user) {
        localStorage.setItem(key, JSON.stringify(user))
    } else {
        localStorage.removeItem(key)
    }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [user, setUser] = React.useState<AuthUser | null>(getStoredUser())
    const isAuthenticated = !!user

    const logout = React.useCallback(async () => {
        await AuthApi.getV1authlogout()

        setStoredUser(null)
        setUser(null)
    }, [])

    const login = React.useCallback(async () => {
        await AuthApi.getV1authlogin()
    }, [])

    React.useEffect(() => {
        fetchAndSetUser(setUser);
    }, [])

    return (
        <AuthContext.Provider value={{ isAuthenticated, user, login, logout }}>
            {children}
        </AuthContext.Provider>
    )
}

export function useAuth() {
    const context = React.useContext(AuthContext)
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider')
    }
    return context
}
