import { exec } from "child_process";

console.log("Installing dependencies...");

let denoInstalled = true;

//Check if deno is installed
exec("deno --version", (error: any, stdout: string, stderr: string) => {
    if (error) {
        denoInstalled = false;
        return;
    }
});

if (!denoInstalled) {
    //if windows
    if (process.platform === "win32") {
        console.log("Deno is not installed, installing...");
        exec("iwr https://deno.land/x/install/install.ps1 -useb | iex");
    } else if (process.platform === "linux" || process.platform === "darwin") {
        console.log("Deno is not installed, installing...");
        exec("curl -fsSL https://deno.land/x/install/install.sh | sh");
    } else {
        console.error("Unsupported platform");
    }
}

let nbgvInstalled = true;

//Check if nbgv is installed
exec("dotnet tool list -g | findstr nbgv", (error: any, stdout: string, stderr: string) => {
    if (error) {
        nbgvInstalled = false;
        return;
    }
});

if (!nbgvInstalled) {
    console.log("nbgv is not installed, installing...");
    exec("dotnet tool install nbgv");
}

exec("npm install", (error: any, stdout: any, stderr: any) => {
    if (error) {
        console.error("Error installing frontend dependencies");
        return;
    }
});

console.log("Dependencies installed!");