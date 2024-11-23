import { makeApi, Zodios, type ZodiosOptions } from "@zodios/core";
import { z } from "zod";

const endpoints = makeApi([
  {
    method: "post",
    path: "/api/v1/docker/push",
    alias: "postApiv1dockerpush",
    requestFormat: "json",
    parameters: [
      {
        name: "x-api-key",
        type: "Header",
        schema: z.string(),
      },
      {
        name: "x-app-name",
        type: "Header",
        schema: z.string(),
      },
    ],
    response: z.void(),
    errors: [
      {
        status: 400,
        description: `Bad Request`,
        schema: z.void(),
      },
      {
        status: 401,
        description: `Unauthorized`,
        schema: z.void(),
      },
    ],
  },
]);

export const DockerApi = new Zodios("http://localhost:1235", endpoints);

export function createApiClient(baseUrl: string, options?: ZodiosOptions) {
  return new Zodios(baseUrl, endpoints, options);
}
