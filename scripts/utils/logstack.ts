export class LogStack {
    private stack: string[] = [];
    constructor(private name: string, stackSize: number) {
        this.stack = new Array(stackSize).fill("");
    }

    push(text: string) {
        this.stack.shift();
        this.stack.push(text);
    }

    getStackAsString() {
        return this.stack.filter(text => text !== "").join("\n");
    }
}