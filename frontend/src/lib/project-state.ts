import { components } from "@/generated";

export enum ProjectState {
    Unknown = "Unknown",
    Running = "Running",
    Starting = "Starting",
    Stopped = "Stopped",
    Deleting = "Deleting"
}

export function getProjectStateFromNumber(stateNum?: components["schemas"]["ProjectState"]): ProjectState {
    switch (stateNum) {
        case "Unknown": return ProjectState.Unknown;
        case "Running": return ProjectState.Running;
        case "Starting": return ProjectState.Starting;
        case "Stopped": return ProjectState.Stopped;
        case "Deleting": return ProjectState.Deleting;
        default: return ProjectState.Unknown;
    }
}