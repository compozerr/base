import { onEphemeralValue, useEphemeralStorage } from "./use-ephemeral-storage.hook";

type N8nIntentEphemeralStorageType = {
	action: "created",
	projectId: string,
	timestamp: number
}
	|
{
	action: "create"
};

const n8nIntentKey = 'n8nIntent-ephemeral';

export function useN8nIntentEphemeralStorage() {
	const hook = useEphemeralStorage<N8nIntentEphemeralStorageType>(n8nIntentKey);
	return {
		setN8nIntentValue: hook.setValue,
		collectN8nIntent: hook.collect
	}
}

export function onN8nIntent(callback: (value: N8nIntentEphemeralStorageType) => void | Promise<void>) {
	return onEphemeralValue<N8nIntentEphemeralStorageType>(n8nIntentKey, callback);
}