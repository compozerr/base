import { DeploymentStatus } from "./deployment-status"

export const getStatusDot = (status: DeploymentStatus) => {
    switch (status) {
        case DeploymentStatus.Completed:
            return <span className="h-2 w-2 rounded-full bg-green-500 mr-2"></span>
        case DeploymentStatus.Deploying:
            return <span className="h-2 w-2 rounded-full bg-blue-500 mr-2"></span>
        case DeploymentStatus.Failed:
            return <span className="h-2 w-2 rounded-full bg-red-500 mr-2"></span>
        case DeploymentStatus.Queued:
            return <span className="h-2 w-2 rounded-full bg-gray-500 mr-2"></span>
        case DeploymentStatus.Cancelled:
            return <span className="h-2 w-2 rounded-full bg-gray-700 mr-2"></span>
        default:
            return <span className="h-2 w-2 rounded-full bg-gray-500 mr-2"></span>
    }
}