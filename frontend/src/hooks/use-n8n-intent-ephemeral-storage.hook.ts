import { useEphemeralStorage } from "./use-storage.hook";

type N8nIntentEphemeralStorageType = {
	action: "created",
	projectId: string,
	timestamp: number
}
	|
{
	action: "create"
};

export function useN8nIntentEphemeralStorage() {
	const key = 'n8nIntent-ephemeral';
	const hook = useEphemeralStorage<N8nIntentEphemeralStorageType>(key);
	return {
		setN8nIntentValue: hook.setValue,
		collectN8nIntent: hook.collect
	}
}