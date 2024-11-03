import { Config } from "./config.ts";

let isShuttingDown = false;

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


                // Check for "Now listening" message and attach debugger if in VS Code
                if (label === "BACKEND" && text.includes("Now listening on:") && Deno.env.get("TERM_PROGRAM") === "vscode") {
                    await Deno.mkdir("./bin", { recursive: true });

                    await Deno.writeTextFile("./bin/backendProcess.pid", backendProcess.pid.toString());
                }

                await Deno.stdout.write(encoder.encode(outputColor));
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

    console.log("Old ports cleaned up.");
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
    args: ["-c", "cd src/backend && dotnet watch run --urls http://localhost:" + Config.ports.backend],
    stdout: "piped",
    stderr: "piped",
}).spawn();

const cleanup = async () => {
    isShuttingDown = true;
    console.log("\nShutting down...");
    terminateProcess(frontendProcess, "Frontend");
    terminateProcess(backendProcess, "Backend");

    await Deno.remove("./bin", { recursive: true });
    Deno.exit(0);
};

Deno.addSignalListener("SIGINT", cleanup);

const frontendPromise = runCommandWithLabel(frontendProcess, "FRONTEND", FRONTEND_COLOR);
const backendPromise = runCommandWithLabel(backendProcess, "BACKEND", BACKEND_COLOR);

await Promise.all([frontendPromise, backendPromise]);

await cleanup();
