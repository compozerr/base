export enum ProjectState {
    Unknown = "Unknown",
    Running = "Running",
    Building = "Building",
    Stopped = "Stopped"
}

export function getProjectStateFromNumber(stateNum?: number): ProjectState {
    switch (stateNum) {
        case 0: return ProjectState.Unknown;
        case 1: return ProjectState.Running;
        case 2: return ProjectState.Building;
        case 3: return ProjectState.Stopped;
        default: return ProjectState.Unknown;
    }
}