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
    moduleComposeContexts[module.name] = path.join("modules", module.name)
}

const moduleComposeContextsEnvironmentVars = Object.entries(moduleComposeContexts).map(([name, path]) => `COMPOSE_${name.toUpperCase()}_CONTEXT=${path}`).join(" ");

const commandFlags = `-f ${dockerComposeFiles.join(" -f ")}`
const upCommand = `${moduleComposeContextsEnvironmentVars} docker-compose ${commandFlags} up --build`;

console.log({ upCommand })

const process = new Command(upCommand);

Deno.addSignalListener("SIGINT", async () => {
    await process.terminateAsync()
});

await process.spawn();

