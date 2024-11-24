import { makeApi, Zodios, type ZodiosOptions } from "@zodios/core";
import { z } from "zod";

type User = Partial<{
  id: number;
  createdAt: string;
  updatedAt: string | null;
  username: string | null;
  email: string | null;
  passwordHash: string | null;
  userRoles: Array<UserRole> | null;
}>;
type UserRole = Partial<{
  id: number;
  createdAt: string;
  updatedAt: string | null;
  userId: number;
  roleId: number;
  user: User;
  role: Role;
}>;
type Role = Partial<{
  id: number;
  createdAt: string;
  updatedAt: string | null;
  name: string | null;
  description: string | null;
  userRoles: Array<UserRole> | null;
}>;

const User: z.ZodType<User> = z.lazy(() =>
  z
    .object({
      id: z.number().int(),
      createdAt: z.string().datetime({ offset: true }),
      updatedAt: z.string().datetime({ offset: true }).nullable(),
      username: z.string().nullable(),
      email: z.string().nullable(),
      passwordHash: z.string().nullable(),
      userRoles: z.array(UserRole).nullable(),
    })
    .partial()
    .strict()
);
const UserRole: z.ZodType<UserRole> = z.lazy(() =>
  z
    .object({
      id: z.number().int(),
      createdAt: z.string().datetime({ offset: true }),
      updatedAt: z.string().datetime({ offset: true }).nullable(),
      userId: z.number().int(),
      roleId: z.number().int(),
      user: User,
      role: Role,
    })
    .partial()
    .strict()
);
const Role: z.ZodType<Role> = z.lazy(() =>
  z
    .object({
      id: z.number().int(),
      createdAt: z.string().datetime({ offset: true }),
      updatedAt: z.string().datetime({ offset: true }).nullable(),
      name: z.string().nullable(),
      description: z.string().nullable(),
      userRoles: z.array(UserRole).nullable(),
    })
    .partial()
    .strict()
);

export const schemas = {
  User,
  UserRole,
  Role,
};

const endpoints = makeApi([
  {
    method: "get",
    path: "/v1",
    alias: "getV1",
    requestFormat: "json",
    response: z.void(),
  },
  {
    method: "get",
    path: "/v1/users/add",
    alias: "getV1usersadd",
    requestFormat: "json",
    response: User,
  },
]);

export const HomeApi = new Zodios("http://localhost:1235", endpoints);

export function createApiClient(baseUrl: string, options?: ZodiosOptions) {
  return new Zodios(baseUrl, endpoints, options);
}
