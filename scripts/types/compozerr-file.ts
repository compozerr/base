import { z } from "npm:zod";
import { Config } from "../config.ts";

export const CompozerrFile = z.object({
    id: z.string().optional(),
    type: z.enum(["project", "module"]),
    name: z.string(),
    dependencies: z.record(z.string()).optional(),
    start: z.string().optional(),
    startupTimeoutMs: z.number().optional(),
    readyMessage: z.string().optional(),
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