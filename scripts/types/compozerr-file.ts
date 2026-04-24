import { z } from "npm:zod";
import { Config } from "../config.ts";

export const CompozerrFile = z.object({
    id: z.string().optional(),
    type: z.enum(["project", "module"]),
    name: z.string(),
    dependencies: z.record(z.string()).optional(),
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
    backend: z.object({
        defaultEnvProperties: z.record(z.string()).optional(),
    }).optional(),
    frontend: z.object({
        srcDir: z.string().optional().default(Config.defaults.frontend.srcDir),
        routesDir: z.string().optional(),
        routePrefix: z.string().optional(),
        defaultEnvProperties: z.record(z.string()).optional(),
    }).optional().default(Config.defaults.frontend),
    dockerComposeFile: z.string().optional()
}).superRefine((data, ctx) => {
    if (data.type === "project" && !data.id) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: "Project must have an id",
            path: ["id"]
        });
    }
});

export type CompozerrFile = z.infer<typeof CompozerrFile>;