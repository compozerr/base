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

        // Handle errors with detail property directly (ProblemDetailError)
        if (error?.detail) {
            description = error.detail;
            title = error.title || "Error";
        } else if (error?.message) {
            // Try to parse JSON ProblemDetails from error message
            const parsed = ApiErrorHandler.tryParseProblemDetails(error.message);
            if (parsed) {
                title = parsed.title || "Error";
                description = parsed.detail || parsed.title || "An unexpected error occurred";
            } else {
                description = error.message;
            }
        }

        // Strip "Validation errors: " prefix for cleaner display
        if (description.startsWith("Validation errors: ")) {
            description = description.replace("Validation errors: ", "");
        }

        toast({
            variant: "destructive",
            title: title,
            description: description,
        });
    }

    private static tryParseProblemDetails(message: string): ProblemDetail | null {
        try {
            const parsed = JSON.parse(message);
            if (parsed && typeof parsed === "object" && (parsed.title || parsed.detail)) {
                return parsed as ProblemDetail;
            }
        } catch {
            // Not JSON
        }
        return null;
    }
}

export { ProblemDetailError };
