import { makeApi, Zodios, type ZodiosOptions } from "@zodios/core";
import { z } from "zod";

const User = z.object({
  id: z.string(),
  name: z.string(),
  email: z.string(),
  avatarUrl: z.string(),
});

const endpoints = makeApi([
  {
    method: "get",
    path: "/v1/users",
    alias: "getV1users",
    requestFormat: "json",
    response: z.array(User),
  },
]);

export const UsersApi = new Zodios("http://localhost:1235", endpoints);

export function createApiClient(baseUrl: string, options?: ZodiosOptions) {
  return new Zodios(baseUrl, endpoints, options);
}
