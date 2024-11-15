import { globSync } from 'glob';
import fs from 'fs';
import path from "path";


export const getModuleAliases: () => Record<string, string> = () => {
    const compozerrFiles = globSync("../modules/*/compozerr.json");

    let aliases: Record<string, string> = {};

    for (const file of compozerrFiles) {
        const moduleName = file.replace("/compozerr.json", "").split("/").pop();

        aliases[`@${moduleName}`] = path.join(file.replace("compozerr.json", ""), "frontend/src/");
    }
    console.log({ aliases })
    return aliases;
}