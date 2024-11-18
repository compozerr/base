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
        name: "apiKey",
        type: "Header",
        schema: z.string(),
      },
    ],
    response: z.void(),
  },
]);

export const DockerApi = new Zodios("http://localhost:1235", endpoints);

export function createApiClient(baseUrl: string, options?: ZodiosOptions) {
  return new Zodios(baseUrl, endpoints, options);
}
