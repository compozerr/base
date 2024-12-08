import { useState, useEffect } from 'react';
import { loadAuth } from '../auth-loader';
import type { AuthContextType } from '../auth';

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