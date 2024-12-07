import * as React from 'react'

export type AuthUser = {
    id: string;
    email: string;
    name: string;
}

export interface AuthContext {
    isAuthenticated: boolean
    login: () => Promise<void>
    logout: () => Promise<void>
    user: AuthUser | null
}

const AuthContext = React.createContext<AuthContext | null>(null)

const key = 'tanstack.auth.user.id'

function getStoredUser() {
    return JSON.parse(localStorage.getItem(key) || 'null') as AuthUser | null
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
        // const response = await AuthApi.logout()

        setStoredUser(null)
        setUser(null)
    }, [])

    const login = React.useCallback(async () => {
        // const response = await AuthApi.login()
        const user = {
            id: '1',
            email: 'abc@emal.com',
            name: 'abc',
        }

        setStoredUser(user)
        setUser(user)
    }, [])

    React.useEffect(() => {
        setUser(getStoredUser())
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
