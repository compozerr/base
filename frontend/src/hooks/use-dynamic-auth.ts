import { useState, useEffect } from 'react';
import type { AuthContextType } from '../auth-mock';

async function loadAuth() {
    try {
        // Use a dynamic import with a URL that Vite can resolve at build time
        const moduleUrl = new URL(
            '../../../modules/auth/frontend/src/auth',
            import.meta.url
        ).href;
        
        const { AuthProvider, useAuth } = await import(/* @vite-ignore */moduleUrl);
        console.log('Auth module loaded');
        return { AuthProvider, useAuth };
    } catch (error) {
        const { AuthProvider, useAuth } = await import('../auth-mock');
        console.warn('Auth module not added', error);
        return { AuthProvider, useAuth };
    }
}
/**
 * Custom hook to dynamically load authentication components. If not included auth module, it will use the mock auth. 
 *
 * @returns An object containing:
 * - `authComponents`: The dynamically loaded authentication components, or `null` if not yet loaded.
 * - `isLoading`: A boolean indicating whether the authentication components are currently being loaded.
 * - `error`: An error object if there was an error loading the authentication components, or `null` if no error occurred.
 */
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