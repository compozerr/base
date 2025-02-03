import { createAPIClient } from "./generated";
import { OperationSchema, requestFn, RequestFnInfo, RequestFnOptions } from "@openapi-qraft/react";
import { QueryClient } from "@tanstack/react-query";

export const queryClient = new QueryClient();

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