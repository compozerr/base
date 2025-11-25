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
	const { collect, setValue } = useEphemeralStorage<T>(key);

	useEffect(() => {
		const execute = async () => {
			const value = collect();
			if (value) {
				setValue(value); // Put it back if it doesn't get processed
				await callback(value);
				collect(); // Collect after all listeners have processed it
			}
		};
		execute();
	}, []);
}