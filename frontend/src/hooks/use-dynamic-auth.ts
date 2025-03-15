import { useState, useEffect } from 'react';
import type { AuthContextType } from '../auth-mock';
import React from 'react';

async function loadAuth() {
    try {
        // Instead of constructing URL, use direct import with explicit path
        //@ts-ignore
        const { AuthProvider, useDynamicAuth } = await import('../../../modules/auth/frontend/src/auth');
        console.log('Auth module loaded');
        return { AuthProvider, useDynamicAuth };
    } catch (error) {
        console.warn('Auth module not added, falling back to mock', error);
        const { AuthProvider, useDynamicAuth } = await import('../auth-mock');
        return { AuthProvider, useDynamicAuth };
    }
}

let useAuth: () => AuthContextType;

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
                    useAuth = components.useDynamicAuth;
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

export { useAuth };