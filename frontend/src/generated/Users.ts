import { makeApi, Zodios, type ZodiosOptions } from "@zodios/core";
import { z } from "zod";

const CreateUserRequest = z
  .object({ email: z.string().nullable(), avatarUrl: z.string().nullable() })
  .partial()
  .strict();

export const schemas = {
  CreateUserRequest,
};

const endpoints = makeApi([
  {
    method: "get",
    path: "/v1/users",
    alias: "getV1users",
    requestFormat: "json",
    response: z.array(User),
  },
  {
    method: "post",
    path: "/v1/users",
    alias: "postV1users",
    requestFormat: "json",
    parameters: [
      {
        name: "body",
        type: "Body",
        schema: CreateUserRequest,
      },
    ],
    response: z.void(),
  },
]);

export const UsersApi = new Zodios("http://localhost:1235", endpoints);

export function createApiClient(baseUrl: string, options?: ZodiosOptions) {
  return new Zodios(baseUrl, endpoints, options);
}
