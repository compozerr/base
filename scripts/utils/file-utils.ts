export class FileUtils {
    public static existsAsync = async (path: string) => {
        try {
            await Deno.lstat(path);
            return true;
        } catch (error) {
            if (error instanceof Deno.errors.NotFound) return false;
            throw error;
        }
    }
}