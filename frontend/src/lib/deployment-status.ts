export enum DeploymentStatus {
    Unknown = 0,
    Deploying = 1,
    Completed = 2,
    Queued = 3,
    Failed = 4
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
