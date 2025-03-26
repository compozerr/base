export enum DeploymentStatus {
    Unknown = "Unknown",
    Deploying = "Deploying",
    Completed = "Completed",
    Queued = "Queued",
    Failed = "Failed"
}

export function getDeploymentStatusFromNumber(stateNum?: number): DeploymentStatus {
    switch (stateNum) {
        case 0: return DeploymentStatus.Unknown;
        case 1: return DeploymentStatus.Deploying;
        case 2: return DeploymentStatus.Completed;
        case 3: return DeploymentStatus.Queued;
        case 4: return DeploymentStatus.Failed;
        default: return DeploymentStatus.Unknown;
    }
}

