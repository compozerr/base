import { CompozerrFile } from "../../types/compozerr-file.ts";

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

        const moduleFolders = Deno.readDir("../modules");

        for await (const moduleFolder of moduleFolders) {
            if (!moduleFolder.isDirectory) continue;

            const file = await Deno.readFile(`../modules/${moduleFolder.name}/compozerr.json`);
            if (!file) throw new Error("compozerr.json not found");
            const decoder = new TextDecoder();
            const compozerr = CompozerrFile.safeParse(JSON.parse(decoder.decode(file)));

            if (!compozerr.success) throw new Error(`compozerr.json is invalid in ${moduleFolder.name}`, { cause: compozerr.error.message });

            for (const [module, _version] of Object.entries(compozerr.data.dependencies)) {
                this.addedModules.push({
                    name: module,
                    config: compozerr.data
                });
            }
        }

        this.isInitialized = true;
    }

    async getAllAddedModulesAsync() {
        if (!this.isInitialized) await this.initializeAsync();
        return this.addedModules;
    }

    async getModulesWithStartCommandsAsync() {
        const modules = await this.getAllAddedModulesAsync();
        return modules.filter(module => !!module.config.start?.trim());
    }
}
