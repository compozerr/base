export enum ProjectStateFilter {
    None = 0,
    Unknown = 1 << 0,
    Running = 1 << 1,
    Starting = 1 << 2,
    Stopped = 1 << 3,
    Deleting = 1 << 4,
    All = Unknown | Running | Starting | Stopped | Deleting
}
