import path from "path";
import fs from "fs";
import { exec } from "child_process";
import { promisify } from "util";

const execAsync = promisify(exec);

const root = path.resolve(__dirname, '../../../..');

const inputArgs = {
    frontendExportFolder: path.join(root, process.argv[2] || "frontend/src/email-templates"),
    backendExportFolder: path.join(root, process.argv[3] || "backend/Api/Emails"),
};

async function copyFolderAsync(source: string, destination: string) {
    if (!fs.existsSync(destination)) {
        await fs.promises.mkdir(destination, { recursive: true });
    }
    const files = await fs.promises.readdir(source);
    for (const file of files) {
        const srcFile = path.join(source, file);
        const destFile = path.join(destination, file);
        if ((await fs.promises.stat(srcFile)).isDirectory()) {
            await copyFolderAsync(srcFile, destFile);
        } else {
            await fs.promises.copyFile(srcFile, destFile);
        }
    }
}

async function createBackendExportFolderExistsAsync() {
    if (!fs.existsSync(inputArgs.backendExportFolder)) {
        await fs.promises.mkdir(inputArgs.backendExportFolder, { recursive: true });
        console.error(`Backend export folder did not exist and was created: ${inputArgs.backendExportFolder}`);
    }
}

async function createFrontendExportFolderExistsAsync() {
    if (!fs.existsSync(inputArgs.frontendExportFolder)) {
        await fs.promises.mkdir(inputArgs.frontendExportFolder, { recursive: true });

        // await execAsync(`npx email export --dir ./src/emails --outDir ${inputArgs.frontendExportFolder} --pretty`);
        await copyFolderAsync(
            path.join(__dirname, '../src/emails'),
            inputArgs.frontendExportFolder
        );
    }

    await execAsync(`npx email export --dir ${inputArgs.frontendExportFolder} --outDir ${inputArgs.backendExportFolder} --pretty`);
}

function convertToPascalCase(str: string): string {
    return str.replace(/(?:^\w|[A-Z]|\b\w|\s+|-)/g, (match, index) => {
        if (+/\s|-/.test(match)) {
            return '';
        }
        if (index === 0) {
            return match.toUpperCase();
        }
        return match.toUpperCase();
    });
}


function createCSharpTemplate(filePath: string, fieldNames: string[]) {
    const getRequiredStringLine = (fieldName: string) => {
        return `    public required string ${fieldName} { get; init; }
`;
    }

    return `using Mail;

namespace Api.Emails;

public sealed class ${convertToPascalCase(filePath)}Template : ReactEmailTemplate
{
    public ${convertToPascalCase(filePath)}Template() : base("Emails/${filePath}.html") { }

${fieldNames.map(getRequiredStringLine).join('')}
}`;
};

async function saveFileAsync(filePath: string, content: string) {
    await fs.promises.writeFile(filePath, content, 'utf-8');
}

async function generateBackendDtosAsync() {
    const htmlFiles = (await fs.promises.readdir(inputArgs.backendExportFolder)).filter(file => file.endsWith('.html'));

    const fileMap: {
        fileName: string;
        replacements: {
            [key: string]: {
                replacementKey: string;
                fieldName: string;
            }
        };
    }[] = [];

    for (const file of htmlFiles) {
        const filePath = path.join(inputArgs.backendExportFolder, file);
        const content = await fs.promises.readFile(filePath, 'utf-8');
        const fileName = path.basename(file, '.html');
        const replacements: {
            [key: string]: {
                replacementKey: string;
                fieldName: string;
            }
        } = {};
        const regex = /%\s*(\w+)\s*%/g;

        let match;

        while ((match = regex.exec(content)) !== null) {
            const key = match[1] as string;
            replacements[key] = {
                replacementKey: `% ${key} %`,
                fieldName: convertToPascalCase(key),
            };
        }

        fileMap.push({ fileName, replacements });
    }

    await Promise.all(fileMap.map(async ({ fileName, replacements }) => {
        const cSharpTemplate = createCSharpTemplate(fileName, Object.values(replacements).map(r => r.fieldName));
        await saveFileAsync(path.join(inputArgs.backendExportFolder, `${convertToPascalCase(fileName)}.cs`), cSharpTemplate);
    }));
}

(async function main() {
    await createBackendExportFolderExistsAsync();
    await createFrontendExportFolderExistsAsync();

    await generateBackendDtosAsync();
})();