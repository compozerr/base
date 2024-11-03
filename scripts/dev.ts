const runCommandWithLabel = async (command: string, label: string, color: string) => {
    const process = new Deno.Command(
        command,
        {
            stdout: "piped",
            stderr: "piped",
        });

    const encoder = new TextEncoder();
    const decoder = new TextDecoder();

    // Read output from the command
    while (true) {
        const output = await process.output();
        if (output.stderr.length === 0 && output.stdout.length === 0) {
            break; // No more output
        }

        // Log the output with the label and color
        if (output.stdout.length > 0) {
            const text = decoder.decode(output.stdout);
            Deno.stdout.write(encoder.encode(`${color}${label}: ${text}\x1b[0m`));
        }

        if (output.stderr.length > 0) {
            const errorText = decoder.decode(output.stderr);
            Deno.stderr.write(encoder.encode(`${color}${label} ERROR: ${errorText}\x1b[0m`));
        }
    }

    // Wait for the process to finish
    const status = await process.output();
    return status.success;
};

// Function to terminate a process
const terminateProcess = async (process: Deno.Command) => {
    try {
        await process.output(); // Wait for it to exit
    } catch (_err: unknown) {
        console.log("Process terminated.");
    }
};

// Commands to run the frontend and backend
const frontendCommand = `cd ${Deno.cwd()}/src/frontend && npm run dev`;
const backendCommand = `cd ${Deno.cwd()}/src/backend && dotnet run`;

// Colors for labeling output
const FRONTEND_COLOR = "\x1b[32m"; // Green
const BACKEND_COLOR = "\x1b[34m";   // Blue

// Run commands concurrently
const frontendProcess = new Deno.Command(frontendCommand, {
    stdout: "piped",
    stderr: "piped",
});
const backendProcess = new Deno.Command(backendCommand, {
    stdout: "piped",
    stderr: "piped",
});

// Capture the SIGINT signal for cleanup
const cleanup = async () => {
    console.log("\nShutting down...");
    await terminateProcess(frontendProcess);
    await terminateProcess(backendProcess);
    Deno.exit(0);
};

Deno.addSignalListener("SIGINT", cleanup);

// Read and log output from both processes concurrently
const frontendPromise = runCommandWithLabel(frontendCommand, "FRONTEND", FRONTEND_COLOR);
const backendPromise = runCommandWithLabel(backendCommand, "BACKEND", BACKEND_COLOR);

// Wait for both processes to complete
await Promise.all([frontendPromise, backendPromise]);

// Cleanup in case the processes exit normally
await cleanup();
