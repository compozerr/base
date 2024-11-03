const runCommandWithLabel = async (process: Deno.ChildProcess, label: string, color: string) => {
    const encoder = new TextEncoder();
    const decoder = new TextDecoder();

    const handleOutput = async (stream: ReadableStream<Uint8Array> | null, isError = false) => {
        if (!stream) return;

        const reader = stream.getReader();
        try {
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                const text = decoder.decode(value);
                const outputLabel = isError ? `${label} ERROR: ` : `${label}: `;
                const outputColor = isError ? `\n${color}${outputLabel}${text}\x1b[0m` : `\n${color}${outputLabel}${text}\x1b[0m`;

                // Check for "Now listening" message and attach debugger if in VS Code
                if (label === "BACKEND" && text.includes("Now listening on:") && Deno.env.get("VSCODE_CLI") !== undefined) {
                    const debugProcess = new Deno.Command("dotnet", {
                        args: ["attach", "--process-id", process.pid.toString()],
                        stdout: "inherit",
                        stderr: "inherit"
                    }).spawn();
                    console.log("\x1b[35mAttaching debugger to .NET process...\x1b[0m");
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
    process.kill("SIGTERM");
    console.log(`${name} process terminated.`);
};

const FRONTEND_COLOR = "\x1b[32m"; // Green
const BACKEND_COLOR = "\x1b[34m";   // Blue

const frontendProcess = new Deno.Command("sh", {
    args: ["-c", "cd src/frontend && npm run dev"],
    stdout: "piped",
    stderr: "piped",
}).spawn();

const backendProcess = new Deno.Command("sh", {
    args: ["-c", "cd src/backend && dotnet watch run"],
    stdout: "piped",
    stderr: "piped",
}).spawn();

const cleanup = () => {
    console.log("\nShutting down...");
    terminateProcess(frontendProcess, "Frontend");
    terminateProcess(backendProcess, "Backend");
    Deno.exit(0);
};

Deno.addSignalListener("SIGINT", cleanup);

const frontendPromise = runCommandWithLabel(frontendProcess, "FRONTEND", FRONTEND_COLOR);
const backendPromise = runCommandWithLabel(backendProcess, "BACKEND", BACKEND_COLOR);

await Promise.all([frontendPromise, backendPromise]);

cleanup();
