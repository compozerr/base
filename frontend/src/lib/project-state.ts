import { components } from "@/generated";

export enum ProjectState {
    Unknown = "Unknown",
    Running = "Running",
    Starting = "Starting",
    Stopped = "Stopped",
    Deleting = "Deleting",
    Unhealthy = "Unhealthy"
}

export function getProjectStateFromNumber(stateNum?: components["schemas"]["Api.Data.ProjectState"]): ProjectState {
    switch (stateNum) {
        case "Unknown": return ProjectState.Unknown;
        case "Running": return ProjectState.Running;
        case "Starting": return ProjectState.Starting;
        case "Stopped": return ProjectState.Stopped;
        case "Deleting": return ProjectState.Deleting;
        case "Unhealthy": return ProjectState.Unhealthy;
        default: return ProjectState.Unknown;
    }
}