import { Config } from "../../config.ts";
import { Command } from "../command.ts";

const addApiClientsToFrontend = async () => {
    await new Command(`cd frontend/src && npx swagger-typescript-api -p http://localhost:${Config.ports.backend}/swagger/v1/swagger.json`, "", {
        silent: true
    }).spawn();
}

export const afterRunBackendAsync = async () => {
    await addApiClientsToFrontend();
}
