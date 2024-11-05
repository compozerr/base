import { Random } from "./random.ts";

interface CommandOptions {
    readyMessage?: string;
    port?: string;
}

export class Command {
    private isShuttingDown = false;
    private color: string;
    public isReady = false;
    private process?: Deno.ChildProcess;
    private label: string;

    constructor(private cmd: string, private name: string, private options?: CommandOptions) {
        if (!options?.readyMessage) {
            this.markAsReady();
        }

        this.label = name.toUpperCase();
        this.color = new Random(this.label).getRandomColor();
    }

    markAsShuttingDown() {
        this.isShuttingDown = true;
    }

    private markAsReady() {
        this.isReady = true;
        dispatchEvent(new Event("ready"));
    }

    async cleanupPortAsync() {
        if (!this.options?.port?.trim()) return;

        const process = new Deno.Command("sh", {
            args: ["-c", `lsof -t -i:${this.options.port} | xargs kill -9`],
        });

        await process.output();
    }

    async spawn() {
        this.process = new Deno.Command("sh", {
            args: ["-c", this.cmd],
            stdout: "piped",
            stderr: "piped",
        }).spawn();

        const encoder = new TextEncoder();
        const decoder = new TextDecoder();

        const handleOutput = async (stream: ReadableStream<Uint8Array> | null, isError = false) => {
            if (!stream) return;

            const reader = stream.getReader();
            try {
                while (true && !this.isShuttingDown) {
                    const { done, value } = await reader.read();
                    if (done) break;

                    const text = decoder.decode(value);
                    const outputLabel = isError ? `${this.label} ERROR: ` : `${this.label}: `;
                    const outputColor = isError ? `\n${this.color}${outputLabel}${text}\x1b[0m` : `\n${this.color}${outputLabel}${text}\x1b[0m`;

                    if (this.isReady || Deno.args.includes("--verbose")) {
                        await Deno.stdout.write(encoder.encode(outputColor));
                    }
                    else if (this.options?.readyMessage && text.includes(this.options.readyMessage)) {
                        this.markAsReady();
                        await Deno.stdout.write(encoder.encode(`${this.color}${this.label} is ready${this.options.port?.trim() ? ` on port ${this.options.port}` : ""}\n`));
                    }
                }
            } finally {
                reader.releaseLock();
            }
        };

        await Promise.all([
            handleOutput(this.process.stdout),
            handleOutput(this.process.stderr, true)
        ]);

        const output = await this.process.output();
        return output.success;
    }

    terminate() {
        this.markAsShuttingDown();

        if (!this.process) return;

        try {
            this.process.kill("SIGTERM");
        } catch (error) {
            console.error(`Error terminating ${this.label} process: ${error}`);
        }
        console.log(`${this.label} process terminated.`);
    }
}
