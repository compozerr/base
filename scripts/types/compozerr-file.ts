import { z } from "npm:zod";
import { Config } from "../config.ts";

export const CompozerrFile = z.object({
    name: z.string(),
    dependencies: z.record(z.string()).optional(),
    start: z.string().optional(),
    startupTimeoutMs: z.number().optional(),
    readyMessage: z.string().optional(),
    end: z.string().optional(),
    port: z.string().optional(),
    frontend: z.object({
        srcDir: z.string().optional().default(Config.defaults.frontend.srcDir),
        routesDir: z.string().optional(),
        routePrefix: z.string().optional(),
    }).optional().default(Config.defaults.frontend),
});

export type CompozerrFile = z.infer<typeof CompozerrFile>;