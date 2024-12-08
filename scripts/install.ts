import { exec } from "child_process";

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

const hasDotnetToolAsync = async (command: string) => {
    try {
        await executeCommandAsync(`dotnet tool list -g | grep ${command} || echo "${command} not installed" >&2`)
        return true;
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

    try {
        //Check if deno is installed
        await executeCommandAsync("deno --version");
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

//#region Install frontned dependencies
const installFrontendDependenciesAsync = async () => {
    try {
        await executeCommandAsync("npm install");
        console.log("Frontend dependencies installed");
    }
    catch (error) {
        console.error("Error installing frontend dependencies");
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

    console.log("All dependencies installed!");
})();

