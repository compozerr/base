import { AddedModulesService } from "./utils/added-modules.ts";
import { Config } from "./config.ts";
import { Command } from "./utils/command.ts";
import { Logger } from "./utils/logger.ts";
import { beforeRunFrontendAsync } from "./utils/frontend/before-run-frontend.ts";
import { afterRunBackendAsync } from "./utils/backend/after-run-backend.ts";
import { exportCompozerrSchemaAsync } from "./utils/compozerr-file-schema-generator.ts";

await exportCompozerrSchemaAsync();

const logger = new Logger("", false, "WHITE");
const moduleService = new AddedModulesService();
await moduleService.initializeAsync();

const commands: Command[] = [
    new Command(
        `cd frontend && npm run dev -- --port ${Config.ports.frontend}`,
        "frontend",
        {
            readyMessage: "press h + enter to show help",
            port: Config.ports.frontend,
            startupTimeoutMs: 10000,
            beforeRunAsync: () => beforeRunFrontendAsync(moduleService)
        }
    ),
    new Command(
        `cd backend && export DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 && dotnet watch run --project Api --urls http://localhost:${Config.ports.backend}`,
        "backend",
        {
            readyMessage: "Content root path:",
            port: Config.ports.backend,
            startupTimeoutMs: 30000,
            logCallback: (text) => {
                if (text.includes("Content root path:")) {
                    setTimeout(() => {
                        logger.logAsync("Click 'F5' to reattach the debugger\n", "RED");
                    }, 1000);
                }
            },
            afterRunAsync: afterRunBackendAsync
        }
    )
];

let isCleaningUp = false;

const cleanupAsync = async () => {
    if (isCleaningUp) return;
    isCleaningUp = true;
    await logger.logAsync("\nShutting down...\n");
    await Promise.all(commands.map(async command => await command.terminateAsync()));

    await logger.logAsync("Cleaning up ports...\n");
    await Promise.all(commands.map(async command => await command.cleanupPortAsync()));
    console.log("Cleaning up ports done");
    Deno.exit(0);
};

Deno.addSignalListener("SIGINT", cleanupAsync);
Command.terminateAllCallback = cleanupAsync;

const modulesWithStartCommands = await moduleService.getModulesWithStartCommandsAsync();

for (const module of modulesWithStartCommands) {
    commands.push(new Command(
        `cd modules/${module.name} && ${module.config.start}`,
        module.name,
        {
            readyMessage: module.config.readyMessage,
            port: module.config.port,
            startupTimeoutMs: module.config.startupTimeoutMs,
            endCommand: module.config.end
        }
    ));
}

addEventListener("ready", async () => {
    if (commands.every(command => command.isReady)) {
        await logger.logAsync("\nAll services are ready\n");
        dispatchEvent(new Event("all-services-ready"));
    }
});

await (new Command("clear")).spawn();

await logger.logAsync("Starting services...\n");

await Promise.all(commands.map(command => command.spawn()));

cleanupAsync();