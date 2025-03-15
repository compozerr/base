import { exec } from "child_process";
import path from "path";
import fs from "fs";

//#region Helper functions
const executeCommandAsync = (command: string) => {
    return new Promise((resolve, reject) => {
        exec(command, (error, stdout, stderr) => {
            if (stderr || error) {
                reject(stderr || error);
            }
            else {
                resolve(stdout);
            }
        });
    });
}

let denoInstalled = true;

//Check if deno is installed
exec("deno --version", (error: any, stdout: string, stderr: string) => {
    if (error) {
        denoInstalled = false;
        return;
    }
    catch {
        return false;
    }
}

const installDotnetToolIfNotExistsAsync = async (command: string) => {
    if (!await hasDotnetToolAsync(command)) {
        console.log(`${command} is not installed, installing...`);
        try {

            await executeCommandAsync(`dotnet tool install -g ${command}`);
            console.log(`${command} installed`);
        }
        catch (error) {
            console.error(`Error installing ${command}`);
            throw error;
        }
    }
    else {
        console.log(`${command} is already installed`);
    }
}
//#endregion

//#region Install deno if not exists
const installDenoIfNotExistsAsync = async () => {
    let denoInstalled = true;

//Check if nbgv is installed
exec("dotnet tool list -g | findstr nbgv", (error: any, stdout: string, stderr: string) => {
    if (error) {
        nbgvInstalled = false;
        return;
    }
    catch {
        denoInstalled = false;
    }

    if (!denoInstalled) {
        console.log("Deno is not installed, installing...");
        if (process.platform === "win32") {
            await executeCommandAsync("iwr https://deno.land/x/install/install.ps1 -useb | iex");
        } else if (process.platform === "linux" || process.platform === "darwin") {
            await executeCommandAsync("curl -fsSL https://deno.land/x/install/install.sh | sh");
        } else {
            console.error("Unsupported platform, cannot install deno");
            throw new Error("Unsupported platform");
        }
    }
}
//#endregion

exec("npm install", (error: any, stdout: any, stderr: any) => {
    if (error) {
        console.error("Error installing frontend dependencies");
        throw error;
    }
}
//#endregion

//#region Initial setup
const createEnvFileIfNotExistsAsync = async () => {
    try {
        const envPath = path.join(__dirname, '../backend/.env');
        const envExamplePath = path.join(__dirname, '../backend/.env.example');

        if (!fs.existsSync(envPath)) {
            fs.copyFileSync(envExamplePath, envPath);
            console.log('backend/.env copied from backend/.env.example');
        }
    } catch (error) {
        console.error("Error creating .env file");
        throw error;
    }
}
//#endregion

(async function main() {
    console.log("Installing all dependencies...");

    await installDenoIfNotExistsAsync();

    await installDotnetToolIfNotExistsAsync("nbgv");
    await installDotnetToolIfNotExistsAsync("microsoft.openapi.kiota");

    await installFrontendDependenciesAsync();

    await createEnvFileIfNotExistsAsync();

    console.log("All dependencies installed!");
})();

