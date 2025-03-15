import { useState, useEffect } from 'react';
import type { AuthContextType } from '../auth-mock';

async function loadAuth() {
    try {
        const moduleUrl = new URL('../../../modules/auth/frontend/src/auth').href;
        // Instead of constructing URL, use direct import with explicit path
        const { AuthProvider, useDynamicAuth } = await import(/* @vite-ignore */moduleUrl);
        console.log('Auth module loaded');
        return { AuthProvider, useDynamicAuth };
    } catch (error) {
        console.warn('Auth module not added, falling back to mock', error);
        const { AuthProvider, useDynamicAuth } = await import('../auth-mock');
        return { AuthProvider, useDynamicAuth };
    }
}
/**
 * Custom hook to dynamically load authentication components.
 * Falls back to mock auth if the auth module is not available.
 *
 * @returns {Object} An object containing:
 * - `authComponents`: The loaded authentication components, or `null` if not yet loaded
 * - `isLoading`: Boolean indicating if components are being loaded
 * - `error`: Error object if loading failed, or `null` if successful
 */
export function useDynamicAuth() {
    const [authComponents, setAuthComponents] = useState<{
        AuthProvider: React.ComponentType<{ children: React.ReactNode }>;
        useDynamicAuth: () => AuthContextType;
    } | null>(null);

    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<Error | null>(null);

    useEffect(() => {
        let mounted = true;

        const loadComponents = async () => {
            try {
                const components = await loadAuth();
                if (mounted) {
                    setAuthComponents(components);
                    setIsLoading(false);
                }
            } catch (err) {
                if (mounted) {
                    console.error("Error loading auth components:", err);
                    setError(err instanceof Error ? err : new Error(String(err)));
                    setIsLoading(false);
                }
            }
        };

        loadComponents();

        return () => {
            mounted = false;
        };
    }, []);

    return {
        authComponents,
        isLoading,
        error
    };
}