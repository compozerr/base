export class Formatter {
    public static fromDate(date?: Date | string, format: "long" | "short" = "short") {
        if (typeof date === "string") {
            date = new Date(date);
        }

        if (!date) return "";

        switch (format) {
            case "long":
                return `${date.getHours().toString().padStart(2, "0")}:${date.getMinutes().toString().padStart(2, "0")} ${date.getDate().toString().padStart(2, "0")}.${(date.getMonth() + 1).toString().padStart(2, "0")}.${date.getFullYear()}`;
            case "short":
                return `${date.getDate().toString().padStart(2, "0")}.${(date.getMonth() + 1).toString().padStart(2, "0")}.${date.getFullYear()}`;
        }

    }
}