const Storage = {
    getItem<T>(key: string, storage: Storage): T | null {
        if (typeof window === "undefined") {
            console.error('Storage is not available when window is undefined');
            return null;
        }

        const item = storage.getItem(key);
        try {
            return item ? JSON.parse(item) : null;
        } catch (error) {
            console.error(`Error parsing item with key ${key}`, error);
            return null;
        }
    },
    setItem<T>(key: string, value: T, storage: Storage) {
        if (typeof window === "undefined") {
            console.error('Storage is not available when window is undefined');
            return;
        }

        try {
            if (typeof value === "string") {
                storage.setItem(key, value);
            } else {
                storage.setItem(key, JSON.stringify(value));
            }
        } catch (error) {
            console.error(`Error setting item with key ${key}`, error);
        }
    },
    removeItem(key: string, storage: Storage) {
        if (typeof window === "undefined") {
            console.error('Storage is not available when window is undefined');
            return;
        }

        storage.removeItem(key);
    }
}

export const SessionStorage = {
    getItem<T>(key: string): T | null {
        return Storage.getItem(key, sessionStorage);
    },
    setItem<T>(key: string, value: T) {
        Storage.setItem(key, value, sessionStorage);
    },
    removeItem(key: string) {
        Storage.removeItem(key, sessionStorage);
    }
}

export const LocalStorage = {
    getItem<T>(key: string): T | null {
        return Storage.getItem(key, localStorage);
    },
    setItem<T>(key: string, value: T) {
        Storage.setItem(key, value, localStorage);
    },
    removeItem(key: string) {
        Storage.removeItem(key, localStorage);
    }
}