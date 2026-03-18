import { createAPIClient } from "./generated";
import { OperationSchema, requestFn, RequestFnInfo, RequestFnOptions } from "@openapi-qraft/react";
import { QueryClient } from "@tanstack/react-query";
import { ApiErrorHandler } from "./api-error-handler";
import { getCurrentOrgId } from "./lib/org-storage";

export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 0,
        },
        mutations: {
            onError: ApiErrorHandler.onError
        }
    },
});

export const apiBaseUrl = import.meta.env.VITE_BACKEND_URL;

export const api = createAPIClient({
    requestFn: (schema: OperationSchema, requestInfo: RequestFnInfo, options?: RequestFnOptions) => {
        const orgId = getCurrentOrgId();
        const headers = new Headers(requestInfo.headers);
        if (orgId) {
            headers.set("X-Organization-Id", orgId);
        }
        return requestFn(
            schema,
            { ...requestInfo, credentials: "include", headers },
            { ...options }
        );
    },
    queryClient,
    baseUrl: apiBaseUrl
});