export interface AuthState {
    isAuthenticated: boolean
    user: {
        id: string
        email: string
        name: string
    }
}