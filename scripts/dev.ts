import { AddedModulesService } from "./utils/added-modules.ts";
import { Config } from "./config.ts";
import { Command } from "./utils/command.ts";

const commands: Command[] = [
    new Command(
        `cd src/frontend && npm run dev -- --port ${Config.ports.frontend}`,
        "frontend",
        {
            readyMessage: "press h + enter to show help",
            port: Config.ports.frontend
        }
    ),
    new Command(
        `cd src/backend && export DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 && dotnet watch run --urls http://localhost:${Config.ports.backend}`,
        "backend",
        {
            readyMessage: "Now listening on:",
            port: Config.ports.backend
        }
    )
];

const cleanup = () => {
    console.log("\nShutting down...");
    commands.forEach(command => command.terminate());
    Deno.exit(0);
};

Deno.addSignalListener("SIGINT", cleanup);

const moduleService = new AddedModulesService();
await moduleService.initializeAsync();

const modulesWithStartCommands = await moduleService.getModulesWithStartCommandsAsync();

for (const module of modulesWithStartCommands) {
    commands.push(new Command(`cd modules/${module.name} && ${module.config.start}`, module.name, { readyMessage: module.config.readyMessage, port: module.config.port }));
}

addEventListener("ready", () => {
    if (commands.every(command => command.isReady)) {
        console.log("All services are ready");
    }
});

console.log("Starting services...");

await Promise.all(commands.map(command => command.cleanupPortAsync()));
await Promise.all(commands.map(command => command.spawn()));

cleanup();