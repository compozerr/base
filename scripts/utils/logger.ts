import { Random } from "./random.ts";

const encoder = new TextEncoder();

export enum LoggerColors {
    RED = "31",
    WHITE = "37",
}

export class Logger {
    private color: string;

    constructor(private label: string, color?: keyof typeof LoggerColors) {
        if (color) {
            this.color = `\x1b[${LoggerColors[color]}m`;
        } else {
            this.color = new Random(label).getRandomColor();
        }
    }

    async logAsync(message: string, color?: string, label?: string) {
        label = label ?? this.label;
        const hasLabel = !!label.trim();
        
        const output = `\n${color ?? this.color}${label}${hasLabel ? ": " : ""}${message}\x1b[0m`;

        await Deno.stdout.write(encoder.encode(output));
    }

    async errorAsync(message: string) {
        await this.logAsync(message, LoggerColors.RED, `${this.label} ERROR`)
    }
}