import { makeApi, Zodios, type ZodiosOptions } from "@zodios/core";
import { z } from "zod";

const MeResponse = z
  .object({
    userId: z.string().uuid(),
    name: z.string().nullable(),
    email: z.string().nullable(),
    avatarUrl: z.string().nullable(),
  })
  .partial()
  .strict();

export const schemas = {
  MeResponse,
};

const endpoints = makeApi([
  {
    method: "get",
    path: "/v1/auth/login",
    alias: "getV1authlogin",
    requestFormat: "json",
    response: z.void(),
  },
  {
    method: "get",
    path: "/v1/auth/logout",
    alias: "getV1authlogout",
    requestFormat: "json",
    response: z.void(),
  },
  {
    method: "get",
    path: "/v1/auth/whoami",
    alias: "getV1authwhoami",
    requestFormat: "json",
    response: z.void(),
  },
  {
    method: "get",
    path: "/v1/auth/me",
    alias: "getV1authme",
    requestFormat: "json",
    response: MeResponse,
  },
]);

export const AuthApi = new Zodios("http://localhost:1235", endpoints);

export function createApiClient(baseUrl: string, options?: ZodiosOptions) {
  return new Zodios(baseUrl, endpoints, options);
}
