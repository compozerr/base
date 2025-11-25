import { LocalStorage, SessionStorage } from "@/lib/storage";
import { useState } from "react";

export function useLocalStorage<T>(key: string, initialValue: T) {
	const [storedValue, setStoredValue] = useState<T>(() => {
		if (typeof window === "undefined") {
			return initialValue;
		}
		try {
			const item = LocalStorage.getItem<T>(key);
			return item ?? initialValue;
		} catch (error) {
			console.log(error);
			return initialValue;
		}
	});
	const setValue = (value: T | ((val: T) => T)) => {
		try {
			const valueToStore =
				value instanceof Function ? value(storedValue) : value;
			setStoredValue(valueToStore);
			LocalStorage.setItem(key, valueToStore);
		} catch (error) {
			console.log(error);
		}
	};
	return [storedValue, setValue] as const;
}

export function useSessionStorage<T>(key: string, initialValue: T) {
	const [storedValue, setStoredValue] = useState<T>(() => {
		if (typeof window === "undefined") {
			return initialValue;
		}
		try {
			const item = SessionStorage.getItem<T>(key);
			return item ?? initialValue;
		} catch (error) {
			console.log(error);
			return initialValue;
		}
	});
	const setValue = (value: T | ((val: T) => T)) => {
		try {
			const valueToStore =
				value instanceof Function ? value(storedValue) : value;
			setStoredValue(valueToStore);
			SessionStorage.setItem(key, valueToStore);
		} catch (error) {
			console.log(error);
		}
	};
	return [storedValue, setValue] as const;
}
