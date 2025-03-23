export class Formatter {
    public static fromDate(date?: Date | string) {
        if (typeof date === "string") {
            date = new Date(date);
        }

        if (!date) return "";

        return `${date.getDate().toString().padStart(2, "0")}.${(date.getMonth() + 1).toString().padStart(2, "0")}.${date.getFullYear()}`;
    }
}