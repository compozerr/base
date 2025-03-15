import { Config } from "../../config.ts";
import { Command } from "../command.ts";

const addApiClientsToFrontend = async () => {
    await new Command(`cd frontend && openapi-qraft --plugin tanstack-query-react --plugin openapi-typescript http://localhost:${Config.ports.backend}/swagger/v1/swagger.json --clean -o src/generated`, "", {
        silent: true
    }).spawn();
    // await new Command(`cd frontend && kiota generate --openapi http://localhost:${Config.ports.backend}/swagger/v1/swagger.json --output '/Users/milo/milodev/gits/web/frontend/src/generated' --language TypeScript `, "", {
    //     silent: true
    // }).spawn();
    // await new Command(`cd frontend && npx openapi-zod-client http://localhost:${Config.ports.backend}/swagger/v1/swagger.json --group-strategy=tag-file -o src/generated --export-schemas=true --export-types=true --base-url=http://localhost:${Config.ports.backend} --strict-objects`, "", {
    //     silent: true
    // }).spawn();
}

export const afterRunBackendAsync = async () => {
    await addApiClientsToFrontend();
}
