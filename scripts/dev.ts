import { AddedModulesService } from "./utils/added-modules.ts";
import { Config } from "./config.ts";
import { Command } from "./utils/command.ts";
import { Logger } from "./utils/logger.ts";

const logger = new Logger("", "WHITE");

const commands: Command[] = [
    new Command(
        `cd frontend && npm run dev -- --port ${Config.ports.frontend}`,
        "frontend",
        {
            readyMessage: "press h + enter to show help",
            port: Config.ports.frontend
        }
    ),
    new Command(
        `cd backend && export DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 && dotnet watch run --urls http://localhost:${Config.ports.backend}`,
        "backend",
        {
            readyMessage: "Content root path:",
            port: Config.ports.backend,
            startupTimeoutMs: 10000,
            logCallback: (text) => {
                if (text.includes("Content root path:")) {
                    setTimeout(() => {
                        logger.logAsync("Click 'F5' to reattach the debugger\n", "RED");
                    }, 1000);
                }
            }
        }
    )
];

const cleanupAsync = async () => {
    await logger.logAsync("\nShutting down...\n");
    commands.forEach(command => command.terminate());
    Deno.exit(0);
};

Deno.addSignalListener("SIGINT", cleanupAsync);
Command.terminateAllCallback = cleanupAsync;

const moduleService = new AddedModulesService();
await moduleService.initializeAsync();

const modulesWithStartCommands = await moduleService.getModulesWithStartCommandsAsync();

for (const module of modulesWithStartCommands) {
    commands.push(new Command(
        `cd modules/${module.name} && ${module.config.start}`,
        module.name,
        {
            readyMessage: module.config.readyMessage,
            port: module.config.port,
            startupTimeoutMs: module.config.startupTimeoutMs,
        }
    ));
}

addEventListener("ready", async () => {
    if (commands.every(command => command.isReady)) {
        await logger.logAsync("All services are ready\n");
    }
});

await (new Command("clear")).spawn();

await logger.logAsync("Starting services...\n");

await Promise.all(commands.map(command => command.cleanupPortAsync()));
await Promise.all(commands.map(command => command.spawn()));

cleanupAsync();