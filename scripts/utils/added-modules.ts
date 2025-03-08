import { CompozerrFile } from "../types/compozerr-file.ts";
import { Config } from "../config.ts";

type AddedService = {
    name: string,
    config: CompozerrFile,
}

export class AddedModulesService {
    private addedModules: AddedService[] = [];
    private isInitialized = false;
    constructor() { }

    async initializeAsync() {
        if (this.addedModules.length > 0) return;

        const moduleFolders = Deno.readDir(Config.moduleFolderDir);

        for await (const moduleFolder of moduleFolders) {
            if (!moduleFolder.isDirectory) continue;
            let file;
            try {
                file = await Deno.readFile(`${Config.moduleFolderDir}/${moduleFolder.name}/compozerr.json`);
            }
            catch (_e) {
                console.error(`compozerr.json is not found in ${moduleFolder.name}`);
                continue;
            }

            const decoder = new TextDecoder();
            const compozerr = CompozerrFile.safeParse(JSON.parse(decoder.decode(file)));

            if (!compozerr.success) throw new Error(`compozerr.json is invalid in ${moduleFolder.name}`, { cause: compozerr.error.message });

            this.addedModules.push({
                name: moduleFolder.name,
                config: compozerr.data
            });
        }

        this.isInitialized = true;
    }

    async getAllAddedModulesAsync() {
        if (!this.isInitialized) await this.initializeAsync();
        return this.addedModules;
    }

    async filterAddedModulesAsync(predicate: (module: AddedService) => boolean) {
        const modules = await this.getAllAddedModulesAsync();
        return modules.filter(predicate);
    }

    async getModulesWithStartCommandsAsync() {
        const modules = await this.getAllAddedModulesAsync();
        return modules.filter(module => module.config.start?.trim());
    }

    async getModulesWithDockerComposeFileAsync() {
        const modules = await this.getAllAddedModulesAsync();
        return modules.filter(module => module.config.dockerComposeFile?.trim())
    }
}
