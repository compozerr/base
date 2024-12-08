import * as React from 'react'
import { apiClient } from '../../../../frontend/src/api-client';
import { AuthContextType, AuthUserType } from '../../../../frontend/src/auth';


const AuthContext = React.createContext<AuthContextType | null>(null)

const key = 'auth.user'

async function fetchAndSetUser(setUser: React.Dispatch<React.SetStateAction<AuthUserType | null>>) {
    let response = null;

    try {
        response = await apiClient.v1.auth.me.get();
    } catch (e) { }

    if (!response) {
        setUser(null);
        setStoredUser(null);
        return;
    }

    const user: AuthUserType = {
        id: response.id!,
        email: response.email!,
        name: response.name!,
        avatarUrl: response.avatarUrl!,
    }

    setUser(user)
    setStoredUser(user)
}

export function getStoredUser(): AuthUserType | null {
    const storedUser = localStorage.getItem(key)
    if (storedUser) {
        return JSON.parse(storedUser)
    }
    return null
}

function setStoredUser(user: AuthUserType | null) {
    if (user) {
        localStorage.setItem(key, JSON.stringify(user))
    } else {
        localStorage.removeItem(key)
    }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [user, setUser] = React.useState<AuthUserType | null>(getStoredUser())
    const isAuthenticated = !!user

    const logout = React.useCallback(async () => {
        await apiClient.v1.auth.logout.get();

        setStoredUser(null)
        setUser(null)

        window.location.href = '/'
    }, [])

    const login = React.useCallback(async () => {
        const loginUrl = apiClient.v1.auth.login.toGetRequestInformation().URL;

        console.log('loginUrl', loginUrl)

        const search = new URLSearchParams(window.location.search)
        location.href = loginUrl + '?returnUrl=' + encodeURIComponent(search.get('redirect') || '/')
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
