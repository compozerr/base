import { makeApi, Zodios, type ZodiosOptions } from "@zodios/core";
import { z } from "zod";

const endpoints = makeApi([
  {
    method: "get",
    path: "/",
    alias: "get",
    requestFormat: "json",
    response: z.void(),
  },
  {
    method: "get",
    path: "/example",
    alias: "getExample",
    requestFormat: "json",
    parameters: [
      {
        name: "name",
        type: "Query",
        schema: z.string(),
      },
    ],
    response: z.object({ message: z.string().nullable() }).partial().strict(),
  },
]);

export const ApiApi = new Zodios("http://localhost:1235", endpoints);

export function createApiClient(baseUrl: string, options?: ZodiosOptions) {
  return new Zodios(baseUrl, endpoints, options);
}
