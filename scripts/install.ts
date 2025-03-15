import { exec } from "child_process";
import { promisify } from 'util';

console.log("Installing dependencies...");

const execPromise = promisify(exec);

(async () => {
    //Check if deno is installed
    const { stderr: denoError } = await execPromise("deno --version");

    if (denoError) {
        await execPromise("npm i -g deno");
    } else {
        console.log("Deno is already installed");
    }

    //Check if nbgv is installed
    const { stderr: nbgvError } = await execPromise("dotnet tool list -g | findstr nbgv");

    if (nbgvError) {
        console.log("nbgv is not installed, installing...");
        exec("dotnet tool install nbgv");
    } else {
        console.log("nbgv is already installed");
    }

    const { stderr: npmIError } = await execPromise("npm install");

    if (npmIError) {
        console.error("Error installing dependencies", npmIError);
    } else {
        console.log("Dependencies installed!");
    }
})()