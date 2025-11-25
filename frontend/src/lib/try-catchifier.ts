export const tryCatchify = async <T>(fn: () => T | Promise<T>): Promise<{
    data: T;
    error: null;
} | { error: Error, data: null }
> => {
    try {
        const result = await fn();
        return { data: result, error: null };
    } catch (error) {
        if (error instanceof Error) {
            return { error, data: null };
        }
        return { error: new Error('An unknown error occurred'), data: null };
    }
};