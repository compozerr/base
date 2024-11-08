import { Logger } from "./logger.ts";

interface CommandOptions {
    readyMessage?: string;
    port?: string;
    logCallback?: (message: string) => void;
    startupTimeoutMs?: number;
}

export class Command {
    static terminateAllCallback? = () => { };

    public isReady = false;

    private process?: Deno.ChildProcess;
    private label: string;
    private logger: Logger;

    private isShuttingDown = false;
    private startupStartTime?: Date = undefined;

    constructor(private cmd: string, name?: string, private options?: CommandOptions) {
        if (!options?.readyMessage) {
            this.markAsReady();
        }

        this.options = { ...options, startupTimeoutMs: options?.startupTimeoutMs ?? 5000 };

        this.label = name?.toUpperCase() ?? "";
        this.logger = new Logger(this.label);
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
        this.startupStartTime = new Date();

        const startupTimeout = setTimeout(() => {
            if (!this.isReady) {
                this.logger.errorAsync(`Process startup took too long (more than ${this.options?.startupTimeoutMs}ms). Terminating all processes...`);
                Command.terminateAllCallback?.();
            }

        }, this.options?.startupTimeoutMs!);

        this.process = new Deno.Command("sh", {
            args: ["-c", this.cmd],
            stdout: "piped",
            stderr: "piped",
        }).spawn();

        const decoder = new TextDecoder();

        const handleOutput = async (stream: ReadableStream<Uint8Array> | null, isError = false) => {
            if (!stream) return;

            const reader = stream.getReader();
            try {
                while (true && !this.isShuttingDown) {
                    const { done, value } = await reader.read();
                    if (done) break;

                    const text = decoder.decode(value);

                    if (this.isReady || Deno.args.includes("--verbose")) {
                        if (this.options?.logCallback) {
                            this.options.logCallback(text);
                        }
                        
                        if (!isError) {
                            await this.logger.logAsync(text);
                        } else {
                            await this.logger.errorAsync(text);
                        }
                    }
                    else if (this.options?.readyMessage && text.includes(this.options.readyMessage)) {
                        const startupTime = new Date().getTime() - this.startupStartTime!.getTime();
                        clearTimeout(startupTimeout);

                        await this.logger.logAsync(`is ready${this.options.port?.trim() ? ` on http://localhost:${this.options.port}` : ""} (took ${startupTime}ms)`);
                        this.markAsReady();
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
            if (error instanceof TypeError && error.message === "Child process has already terminated") {
                console.log(`${this.label} process already terminated.`);
                return;
            }

            console.error(`Error terminating ${this.label} process: ${error}`);
        }
        console.log(`${this.label} process terminated.`);
    }
}
