import { useState, useEffect } from 'react';
import type { AuthContextType } from '../auth';


async function loadAuth() {
    try {
        // Try to import the real auth implementation
        const { AuthProvider, useAuth } = await import('../../../modules/auth/frontend/src/auth');
        console.log('Auth module loaded');
        return { AuthProvider, useAuth };
    } catch (error) {
        const { AuthProvider, useAuth } = await import('../auth');
        console.warn('Auth module not added', error);
        return { AuthProvider, useAuth };
    }
}

export function useDynamicAuth() {
    const [authComponents, setAuthComponents] = useState<{
        AuthProvider: React.ComponentType<{ children: React.ReactNode }>;
        useAuth: () => AuthContextType;
    } | null>(null);

    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<Error | null>(null);

    useEffect(() => {
        loadAuth()
            .then(components => {
                setAuthComponents(components);
                setIsLoading(false);
            })
            .catch(err => {
                console.error("Error loading auth components", err);
                setError(err);
                setIsLoading(false);
            });
    }, []);

    return {
        authComponents,
        isLoading,
        error
    };
}