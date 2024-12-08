import { AnonymousAuthenticationProvider } from "@microsoft/kiota-abstractions";
import { FetchRequestAdapter, HttpClient } from "@microsoft/kiota-http-fetchlibrary";
import { createApiClient } from "./generated/apiClient";

const authProvider = new AnonymousAuthenticationProvider();

const httpClient = new HttpClient((url, requestInit) => {
    return fetch(url, {
        ...requestInit,
        credentials: 'include',
    });
});

const adapter = new FetchRequestAdapter(authProvider, undefined, undefined, httpClient);

export const apiClient = createApiClient(adapter);