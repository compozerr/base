import { z } from "npm:zod";

export const CompozerrFile = z.object({
    dependencies: z.record(z.string()),
    start: z.string().optional(),
    startupTimeoutMs: z.number().optional(),
    readyMessage: z.string().optional(),
    end: z.string().optional(),
    port: z.string().optional()
});

export type CompozerrFile = z.infer<typeof CompozerrFile>;