export class Formatter {
    public static fromDate(date?: Date | string, format: "long" | "short" = "short") {
        if (typeof date === "string") {
            date = new Date(date);
        }

        if (!date || isNaN(date.getTime())) return "";

        const localDate = new Date(date.getTime());

        switch (format) {
            case "long":
                return `${localDate.getDate().toString().padStart(2, "0")}.${(localDate.getMonth() + 1).toString().padStart(2, "0")}.${localDate.getFullYear()} ${localDate.getHours().toString().padStart(2, "0")}:${localDate.getMinutes().toString().padStart(2, "0")}`;
            case "short":
                return `${localDate.getDate().toString().padStart(2, "0")}.${(localDate.getMonth() + 1).toString().padStart(2, "0")}.${localDate.getFullYear()}`;
        }
    }

    /**
     * Converts a duration string into a formatted representation.
     * @param duration - format should be hh:mm:ss.mmmmmmmm
     */
    public static fromDuration(duration: string) {
        const parts = duration.split(":");
        const hours = parseInt(parts[0]!);
        const minutes = parseInt(parts[1]!);
        const seconds = parseInt(parts[2]!.split(".")[0]!);

        const totalMinutes = hours * 60 + minutes + seconds / 60;

        if (Math.round(totalMinutes) > 0) {
            return `${Math.round(totalMinutes)}m`;
        } else {
            return `${Math.round(totalMinutes * 60)}s`;
        }
    }
}