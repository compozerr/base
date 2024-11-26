import { Random } from "./random.ts";

const encoder = new TextEncoder();

export enum LoggerColors {
    RED = "31",
    WHITE = "37",
}

export class Logger {
    private color: string;
    private canLog = true;

    private fromStringToTerminalColor(color: string): string {
        return `\x1b[${color}m`;
    }

    constructor(private label: string, private silent: boolean = false, color?: keyof typeof LoggerColors) {
        if (color) {
            this.color = this.fromStringToTerminalColor(LoggerColors[color]);
        } else {
            this.color = new Random(label).getRandomColor();
        }
    }

    setCanLog(canLog: boolean) {
        this.canLog = canLog;
    }

    async logAsync(message: string, color?: keyof typeof LoggerColors, label?: string) {
        if (this.silent && !Deno.args.includes("--verbose") || !this.canLog) return;

        label = label ?? this.label;
        const hasLabel = !!label.trim();

        const terminalColor = color ? this.fromStringToTerminalColor(LoggerColors[color]) : this.color;

        const output = `\n${terminalColor}${label}${hasLabel ? ": " : ""}${message}\x1b[0m`;

        await Deno.stdout.write(encoder.encode(output));
    }

    async errorAsync(message: string) {
        this.canLog = true;
        await this.logAsync(message, "RED", `${this.label} ERROR`)
    }
}