const openTerminalMac = async (command: string) => {
    const process = Deno.run({
        cmd: [
            "osascript", // AppleScript to open a new terminal window
            "-e",
            `tell application "Terminal" to do script "${command}"`
        ],
    });

    await process.status();
}

const openTerminalWindows = async (command: string) => {
    const process = Deno.run({
        cmd: [
            "cmd", // Command prompt
            "/c",
            `start cmd /k "${command}"` // Start a new command prompt with the specified command
        ],
    });

    await process.status();
}

const openTerminal = async (command: string) => {

    switch (Deno.build.os) {
        case "darwin":
            await openTerminalMac(command);
            break;
        case "win32":
            await openTerminalWindows(command);
            break;
        default:
            console.error("Unsupported platform");
            Deno.exit(1);
    }
};

// Command to run the frontend
const frontendCommand = `cd ${Deno.cwd()}/src/frontend && npm run dev`;

// Command to run the backend
const backendCommand = `cd ${Deno.cwd()}/src/backend && dotnet run`; // Adjust the command if necessary

// Open terminal windows
await openTerminal(frontendCommand);
await openTerminal(backendCommand);
