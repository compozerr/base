import path from "node:path";
import { AddedModulesService } from "./utils/added-modules.ts";
import { Command } from "./utils/command.ts";

const moduleService = new AddedModulesService();

const dockerComposeFiles = ["docker-compose.yml"];

const moduleComposeContexts: { [name: string]: string } = {};

const modulesWithDockerComposeFile = await moduleService.getModulesWithDockerComposeFileAsync();

for (const module of modulesWithDockerComposeFile) {
    const relativePathToDockerComposeFile = path.join("modules", module.name, module.config.dockerComposeFile!);
    dockerComposeFiles.push(relativePathToDockerComposeFile);

    const dockerFilePath = module.config.dockerComposeFile!;
    const lastSlashIndex = dockerFilePath.lastIndexOf('/');
    const relativeContextPath = lastSlashIndex === -1 ? "." : dockerFilePath.substring(0, lastSlashIndex)
    
    moduleComposeContexts[module.name] = path.join("modules", module.name, relativeContextPath)
}

const moduleComposeContextsEnvironmentVars = Object.entries(moduleComposeContexts).map(([name, path]) => `COMPOSE_${name.toUpperCase()}_CONTEXT=${path}`).join(" ");

if (!Deno.args.includes("--no-build")) {
    console.log("🧹 Cleaning old build cache...");
    const cleanCommand = "docker builder prune --filter until=168h --keep-storage=20GB -f";
    const cleanProcess = new Command(cleanCommand);
    try {
        await cleanProcess.spawn();
        console.log("✅ Cache cleanup completed");
    } catch (error) {
        console.log("⚠️  Cache cleanup failed, continuing anyway:", error);
    }
}

const commandFlags = `-f ${dockerComposeFiles.join(" -f ")}`
const upCommand = `${moduleComposeContextsEnvironmentVars} docker-compose ${commandFlags} up${Deno.args.includes("--no-build") ? "" : " --build"}${Deno.args.includes("-d") ? " -d" : ""}`;

console.log({ upCommand })

const process = new Command(upCommand);

Deno.addSignalListener("SIGINT", async () => {
    await process.terminateAsync()
});

await process.spawn();

