import { globSync } from 'glob';
import fs from 'fs';
import path from "path";

type VirtualPhysicalRoute = {
    type: "physical",
    directory: string,
    pathPrefix: string,
}

const getModulesPhysicalRoutes: () => VirtualPhysicalRoute[] = () => {
    const compozerrFiles = globSync("../modules/*/compozerr.json");

    let routes: VirtualPhysicalRoute[] = [];

    for (const file of compozerrFiles) {
        const fileText = fs.readFileSync(file, "utf8");
        const compozerr = JSON.parse(fileText) as { frontend?: { routesDir?: string, routePrefix?: string } };

        if (!compozerr.frontend?.routesDir) continue;

        routes.push({
            type: "physical",
            directory: path.join("../../", file.replace("compozerr.json", ""), compozerr.frontend?.routesDir),
            pathPrefix: compozerr?.frontend?.routePrefix ?? "",
        });
    }

    return routes;
}

export const getVirtualRouteConfig: () => import('@tanstack/virtual-file-routes').VirtualRootRoute = () => {
    return {
        file: "__root.tsx",
        type: "root",
        children: [
            {
                type: "physical",
                directory: ".",
                pathPrefix: "",
            },
            ...getModulesPhysicalRoutes()
        ]
    };
}