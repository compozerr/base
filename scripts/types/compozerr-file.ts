import { z } from "npm:zod";
import { Config } from "../config.ts";

export const CompozerrFile = z.object({
    id: z.string().optional(),
    type: z.enum(["project", "module"]),
    name: z.string(),
    version: z.string().optional(),
    description: z.string().optional(),
    dependencies: z.record(z.string()).optional(),
    /** Alias of `dependencies`. Prefer `dependencies` in new configs. */
    modules: z.record(z.string()).optional(),
    origin: z.object({
        repo: z.string().optional(),
        commit: z.string().optional(),
        addedAt: z.string().optional(),
    }).optional(),
    templateType: z.string().optional(),
    templateFile: z.string().optional(),
    services: z.object({
        start: z.string().optional(),
        stop: z.string().optional(),
        readyCheck: z.string().optional(),
        startupTimeoutMs: z.number().optional(),
        port: z.string().optional(),
    }).optional(),
    /** @deprecated Use `services.start` instead */
    start: z.string().optional(),
    startupTimeoutMs: z.number().optional(),
    /** @deprecated Use `services.readyCheck` instead */
    readyMessage: z.string().optional(),
    /** @deprecated Use `services.stop` instead */
    end: z.string().optional(),
    port: z.string().optional(),
    cwd: z.string().optional(),
    build: z.boolean().optional(),
    zeroDowntime: z.boolean().optional(),
    healthcheck: z.object({
        service: z.string().optional(),
        path: z.string().optional(),
        interval: z.string().optional(),
        timeout: z.string().optional(),
    }).optional(),
    docker: z.object({
        composeCommand: z.string().optional(),
    }).optional(),
    backend: z.object({
        defaultEnvProperties: z.record(z.string()).optional(),
        projects: z.array(z.string()).optional(),
    }).optional(),
    frontend: z.object({
        srcDir: z.string().optional().default(Config.defaults.frontend.srcDir),
        routesDir: z.string().optional(),
        routePrefix: z.string().optional(),
        defaultEnvProperties: z.record(z.string()).optional(),
    }).optional().default(Config.defaults.frontend),
    dockerComposeFile: z.string().optional(),
}).superRefine((data, ctx) => {
    if (data.type === "project" && !data.id) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: "Project must have an id",
            path: ["id"],
        });
    }
});

export type CompozerrFile = z.infer<typeof CompozerrFile>;
