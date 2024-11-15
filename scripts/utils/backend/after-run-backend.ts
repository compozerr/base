import { Config } from "../../config.ts";
import { Command } from "../command.ts";

const addApiClientsToFrontend = async () => {
    await new Command(`cd frontend && npx openapi-zod-client http://localhost:${Config.ports.backend}/swagger/v1/swagger.json --group-strategy=tag-file -o src/generated --export-schemas=true --export-types=true --base-url=http://localhost:${Config.ports.backend} --strict-objects`, "", {
        
    }).spawn();
}

export const afterRunBackendAsync = async () => {
    await addApiClientsToFrontend();
}
