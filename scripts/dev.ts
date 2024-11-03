import { Config } from "./config.ts";

let isShuttingDown = false;
let isFrontendReady = false;
let isBackendReady = false;

const printServicesReady = () => {
    if (!isBackendReady || !isFrontendReady) {
        return;
    }
    
    for (const [key, value] of Object.entries(Config.ports)) {
        console.log(`${key} running on http://localhost:${value}`);
    }

    console.log("All services are ready");
}

const runCommandWithLabel = async (process: Deno.ChildProcess, label: string, color: string) => {
    const encoder = new TextEncoder();
    const decoder = new TextDecoder();

    const handleOutput = async (stream: ReadableStream<Uint8Array> | null, isError = false) => {
        if (!stream) return;

        const reader = stream.getReader();
        try {
            while (true && !isShuttingDown) {
                const { done, value } = await reader.read();
                if (done) break;

                const text = decoder.decode(value);
                const outputLabel = isError ? `${label} ERROR: ` : `${label}: `;
                const outputColor = isError ? `\n${color}${outputLabel}${text}\x1b[0m` : `\n${color}${outputLabel}${text}\x1b[0m`;

                if ((isFrontendReady && isBackendReady) || Deno.args.includes("--verbose")) {
                    await Deno.stdout.write(encoder.encode(outputColor));
                }
                else
                    if (label === "BACKEND") {
                        if (text.includes("Now listening on:")) {
                            setTimeout(() => { isBackendReady = true; printServicesReady(); }, 10);
                        }
                    } else if (label === "FRONTEND") {
                        if (text.includes("press h + enter to show help")) {
                            setTimeout(() => { isFrontendReady = true; printServicesReady(); }, 10);
                        }
                    }
            }
        } finally {
            reader.releaseLock();
        }
    };

    await Promise.all([
        handleOutput(process.stdout),
        handleOutput(process.stderr, true)
    ]);

    const output = await process.output();
    return output.success;
};

const terminateProcess = (process: Deno.ChildProcess, name: string) => {
    try {
        process.kill("SIGTERM");
    } catch (error) {
        console.error(`Error terminating ${name} process: ${error}`);
    }
    console.log(`${name} process terminated.`);
};

const cleanup_old_ports = async () => {
    for (const port of Object.values(Config.ports)) {
        const process = new Deno.Command("sh", {
            args: ["-c", `lsof -t -i:${port} | xargs kill -9`],
            stdout: "piped",
            stderr: "piped"
        });

        await process.output();
    }
}

await cleanup_old_ports();

const FRONTEND_COLOR = "\x1b[32m"; // Green
const BACKEND_COLOR = "\x1b[34m";   // Blue

const frontendProcess = new Deno.Command("sh", {
    args: ["-c", `cd src/frontend && npm run dev -- --port ${Config.ports.frontend}`],
    stdout: "piped",
    stderr: "piped",
}).spawn();

const backendProcess = new Deno.Command("sh", {
    args: ["-c", "cd src/backend && export DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 && dotnet watch run --urls http://localhost:" + Config.ports.backend],
    stdout: "piped",
    stderr: "piped",
}).spawn();

const cleanup = () => {
    isShuttingDown = true;
    console.log("\nShutting down...");
    terminateProcess(frontendProcess, "Frontend");
    terminateProcess(backendProcess, "Backend");

    Deno.exit(0);
};

Deno.addSignalListener("SIGINT", cleanup);

const frontendPromise = runCommandWithLabel(frontendProcess, "FRONTEND", FRONTEND_COLOR);
const backendPromise = runCommandWithLabel(backendProcess, "BACKEND", BACKEND_COLOR);

console.log("Starting services...");

await Promise.all([frontendPromise, backendPromise]);

cleanup();