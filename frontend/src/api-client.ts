import { createAPIClient } from "./generated";
import { OperationSchema, requestFn, RequestFnInfo, RequestFnOptions } from "@openapi-qraft/react";
import { QueryClient } from "@tanstack/react-query";

export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 5 * 60 * 1000 // 5 minutes
        }
    }
});

export const apiBaseUrl = import.meta.env.VITE_BACKEND_URL;

export const api = createAPIClient({
    requestFn: (schema: OperationSchema, requestInfo: RequestFnInfo, options?: RequestFnOptions) => {
        return requestFn(
            schema,
            { ...requestInfo, credentials: "include" },
            { ...options }
        );
    },
    queryClient,
    baseUrl: apiBaseUrl
});