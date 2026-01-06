import { SessionStorage } from "@/lib/storage";
import { useEffect } from "react";

export function useEphemeralStorage<T>(key: string) {
	const setValue = (value: T): void => {
		SessionStorage.setItem(key, value);
	};

	const collect = (): T | null => {
		const value = SessionStorage.getItem<T>(key);
		SessionStorage.removeItem(key);
		return value;
	};

	return { setValue, collect };
}

export function onEphemeralValue<T>(key: string, callback: (value: T) => void | Promise<void>) {
	const { collect } = useEphemeralStorage<T>(key);

	useEffect(() => {
		const execute = async () => {
			const value = collect();
			if (value) {
				await callback(value);
			}
		};
		execute();
	}, []);
}

/**
 * Captures query parameters from the URL, stores them in sessionStorage,
 * and removes them from the URL. Returns the captured values.
 */
export function useEphemeralQueryParams<T extends Record<string, string | boolean | undefined>>(
	paramKeys: (keyof T)[],
	storageKey: string
) {
	const { setValue, collect } = useEphemeralStorage<T>(storageKey);

	useEffect(() => {
		if (typeof window === "undefined") return;

		const url = new URL(window.location.href);
		const capturedParams: Partial<T> = {};
		let hasParams = false;

		for (const key of paramKeys) {
			const value = url.searchParams.get(key as string);
			if (value !== null) {
				hasParams = true;
				if (value === "true") {
					capturedParams[key] = true as T[keyof T];
				} else if (value === "false") {
					capturedParams[key] = false as T[keyof T];
				} else {
					capturedParams[key] = value as T[keyof T];
				}
				url.searchParams.delete(key as string);
			}
		}

		if (hasParams) {
			setValue(capturedParams as T);
			window.history.replaceState({}, "", url.toString());
		}
	}, []);

	return { collect };
}