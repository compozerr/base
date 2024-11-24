import { Logger } from "./logger.ts";

interface CommandOptions {
    readyMessage?: string;
    port?: string;
    logCallback?: (message: string) => void;
    startupTimeoutMs?: number;
    beforeRunAsync?: () => Promise<void>;
    afterRunAsync?: () => Promise<void>;
    silent?: boolean;
    endCommand?: string;
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
        this.logger = new Logger(this.label, this.options.silent);
    }

    markAsShuttingDown() {
        this.isShuttingDown = true;
    }

    private markAsReady() {
        this.isReady = true;
        dispatchEvent(new Event("ready"));
    }

    private checkIfReady(text: string): boolean{
        if(!this.options?.readyMessage) return true;

        if(this.options?.readyMessage?.startsWith("regex:")){
            const regex = new RegExp(this.options.readyMessage.slice(6));
            if(regex.test(text)){
                return true;
            }
        }
        
        if(this.options?.readyMessage && text.includes(this.options.readyMessage)){
            return true;
        }

        return false;
    }
    async cleanupPortAsync() {
        if (!this.options?.port?.trim()) return;
        
        const maxRetries = 3;
        const waitBetweenRetries = 1000; // ms
        
        for (let attempt = 0; attempt < maxRetries; attempt++) {
            try {
                // Use both IPv4 and IPv6 checks with netstat/lsof
                const checkCommands = [
                    // Check using lsof (more details)
                    new Deno.Command("sh", {
                        args: ["-c", `lsof -i :${this.options.port}`],
                    }),
                    // Backup check using netstat
                    new Deno.Command("sh", {
                        args: ["-c", `netstat -an | grep LISTEN | grep ${this.options.port}`],
                    })
                ];
                
                let processFound = false;
                for (const cmd of checkCommands) {
                    try {
                        const result = await cmd.output();
                        const output = new TextDecoder().decode(result.stdout);
                        if (output.trim()) {
                            processFound = true;
                            // Extract PIDs - this handles both lsof and netstat output formats
                            const pids = new Set();
                            output.split('\n').forEach(line => {
                                const parts = line.trim().split(/\s+/);
                                if (parts.length >= 2) {
                                    // For lsof format
                                    const pid = parts[1];
                                    if (/^\d+$/.test(pid)) {
                                        pids.add(pid);
                                    }
                                }
                            });
    
                            // Kill processes
                            for (const pid of pids) {
                                try {
                                    // Try SIGTERM first
                                    const termProcess = new Deno.Command("kill", {
                                        args: [pid as string],
                                    });
                                    await termProcess.output();
                                    
                                    // Wait a bit
                                    await new Promise(resolve => setTimeout(resolve, 500));
                                    
                                    // If still running, use SIGKILL
                                    const checkStillRunning = new Deno.Command("ps", {
                                        args: ["-p", pid as string],
                                    });
                                    const checkResult = await checkStillRunning.output();
                                    if (new TextDecoder().decode(checkResult.stdout).includes(pid as string)) {
                                        const killProcess = new Deno.Command("kill", {
                                            args: ["-9", pid as string],
                                        });
                                        await killProcess.output();
                                    }
                                } catch {
                                    // Process might already be gone
                                }
                            }
                        }
                    } catch {
                        // Command might fail, continue to next check
                    }
                }
    
                // Verify port is actually free by trying to bind to it
                const testBind = new Deno.Command("nc", {
                    args: ["-z", "-v", "localhost", this.options.port],
                });
                
                try {
                    await testBind.output();
                    // If we can still connect, port is not free
                    if (attempt < maxRetries - 1) {
                        await new Promise(resolve => setTimeout(resolve, waitBetweenRetries));
                        continue;
                    }
                    throw new Error(`Port ${this.options.port} is still in use after cleanup attempts`);
                } catch {
                    // If nc fails to connect, port is free
                    return;
                }
                
            } catch (error) {
                if (attempt === maxRetries - 1) {
                    console.error(`Failed to clean up port ${this.options.port}:`, error);
                    throw error;
                }
                await new Promise(resolve => setTimeout(resolve, waitBetweenRetries));
            }
        }
    }

    async spawn() {
        this.startupStartTime = new Date();

        const startupTimeout = setTimeout(() => {
            if (!this.isReady) {
                this.logger.errorAsync(`Process startup took too long (more than ${this.options?.startupTimeoutMs}ms). Terminating all processes...`);
                Command.terminateAllCallback?.();
            }

        }, this.options?.startupTimeoutMs!);

        await this.options?.beforeRunAsync?.();

        this.process = new Deno.Command("sh", {
            args: ["-c", this.cmd],
            stdout: "piped",
            stderr: "piped",
        }).spawn();

        const decoder = new TextDecoder();

        const handleOutput = async (stream: ReadableStream<Uint8Array> | null, isError = false) => {
            if (!stream) return;

            const isDockerCommand = this.cmd.includes("docker"); // Docker commands log progress into stderr which is not an error
            const treatAsError = isError && !isDockerCommand;

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

                    if (!this.isReady && treatAsError) {
                        this.logger.errorAsync(text);
                    }

                    if (!this.isReady && this.checkIfReady(text)) {
                        const startupTime = new Date().getTime() - this.startupStartTime!.getTime();
                        clearTimeout(startupTimeout);

                        await this.options?.afterRunAsync?.();

                        await this.logger.logAsync(`is ready${this.options?.port?.trim() ? ` on http://localhost:${this.options.port}` : ""} (took ${startupTime}ms)`);
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

        if (this.options?.endCommand) {

            const process = new Deno.Command("sh", {
                args: ["-c", this.options.endCommand],
            });
            process.outputSync();
        }

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
