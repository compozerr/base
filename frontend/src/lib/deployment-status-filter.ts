export enum DeploymentStatusFilter {
    None = 0,
    Deploying = 1 << 0,
    Completed = 1 << 1, 
    Queued = 1 << 2,
    Failed = 1 << 3,
    All = Deploying | Completed | Queued | Failed
}
