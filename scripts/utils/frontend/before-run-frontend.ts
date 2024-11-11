import { AddedModulesService } from "../added-modules.ts";
import { Config } from "../../config.ts";
import { FileUtils } from "../file-utils.ts";

const addAliasesToFrontendTsconfig = async (addedModulesService: AddedModulesService) => {
    const modules = await addedModulesService.getAllAddedModulesAsync();
    const denoFile = await Deno.readFile("frontend/tsconfig.modules.json")
    const decoder = new TextDecoder();
    const tsconfig = JSON.parse(decoder.decode(denoFile));

    for (const module of modules) {
        const srcDir = `${Config.moduleFolderDir}/${module.name}/${module.config.frontend.srcDir}`;

        if (!await FileUtils.existsAsync(srcDir)) continue;

        const aliasPath = `../${srcDir}/*`;

        if (!tsconfig.compilerOptions?.paths) tsconfig.compilerOptions = {
            ...tsconfig.compilerOptions,
            paths: {}
        };
        if (!tsconfig.compilerOptions.paths[module.config.frontend.alias!]) tsconfig.compilerOptions.paths[module.config.frontend.alias!] = [];

        if (!tsconfig.compilerOptions.paths[module.config.frontend.alias!].includes(aliasPath))
            tsconfig.compilerOptions.paths[module.config.frontend.alias!].push(aliasPath);
    }

    await Deno.writeFile("frontend/tsconfig.modules.json", new TextEncoder().encode(JSON.stringify(tsconfig, null, 4)));
}

export const beforeRunFrontendAsync = async (addedModulesService: AddedModulesService) => {
    await addAliasesToFrontendTsconfig(addedModulesService);
}
