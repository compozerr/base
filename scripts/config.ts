export const Config = {
    ports: {
        frontend: "1234",
        backend: "1235"
    },
    moduleFolderDir: "modules",
    defaults: {
        frontend: {
            alias: "@/*",
            srcDir: "frontend/src",
        }
    }
}