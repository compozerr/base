import { toast } from "./hooks/use-toast";

interface ProblemDetail {
    detail?: string;
    instance?: string;
    status?: number;
    title?: string;
    traceId?: string;
    type?: string;
}

class ProblemDetailError extends Error {
    detail?: string;
    instance?: string;
    status?: number;
    title: string;
    traceId?: string;
    type?: string;

    constructor(problemDetail: ProblemDetail) {
        super(problemDetail.title || "API Error");
        this.detail = problemDetail.detail;
        this.instance = problemDetail.instance;
        this.status = problemDetail.status;
        this.title = problemDetail.title || "API Error";
        this.traceId = problemDetail.traceId;
        this.type = problemDetail.type;
        this.name = "ProblemDetailError";
    }
}

export class ApiErrorHandler {
    static onError(error: any) {
        let title = "Error";
        let description = "An unexpected error occurred";

        // Handle errors with detail property directly
        if (error?.detail) {
            description = error.detail;
            title = error.title || "Error";
        } else if (error?.message) {
            description = error.message;
        }

        // Show toast notification for validation errors
        if (description.startsWith("Validation errors:")) {
            toast({
                variant: "destructive",
                title: title,
                description: description.replace("Validation errors: ", ""),
            });
        }
    }
}

export { ProblemDetailError };
