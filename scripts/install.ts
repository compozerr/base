import { exec } from "child_process";
import { promisify } from 'util';

console.log("Installing dependencies...");

const execPromise = promisify(exec);

(async () => {
    //Check if deno is installed
    try {
        await execPromise("deno --version");

        console.log("Deno is already installed");
    } catch {
        console.log("Deno not installed, installing now...")
        await execPromise("npm i -g deno");
    }

    const { stderr: npmIError } = await execPromise("npm install");

    if (npmIError) {
        console.error("Error installing dependencies", npmIError);
    } else {
        console.log("Dependencies installed!");
    }

    // Check and copy .env files if they don't exist
    const envPaths = ['./backend', './frontend'];
    for (const path of envPaths) {
        try {
            const { stdout } = await execPromise(`[ ! -f ${path}/.env ] && cp ${path}/.env.example ${path}/.env || echo "exists"`);
            if (stdout.includes('exists')) {
                console.log(`${path}/.env already exists`);
            } else {
                console.log(`Created ${path}/.env from example`);
            }
        } catch (error) {
            console.error(`Error handling .env in ${path}:`, error);
        }
    }
    
})()