import crypto from "node:crypto";

export class Random {
    private seed: string;

    constructor(seed: string) {
        this.seed = seed;
    }

    getRandomColor() {
        const hashed = crypto.createHash("sha256").update(this.seed).digest("hex");
        const color = Math.floor(parseInt(hashed, 16) * 1000) % 7 + 31;
        return `\x1b[${color}m`;
    }
}
