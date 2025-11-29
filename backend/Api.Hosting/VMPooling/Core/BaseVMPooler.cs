using Api.Abstractions;

namespace Api.Hosting.VMPooling.Core;

public abstract class BaseVMPooler
{
    public abstract Task CreateNewInstanceAsync(ProjectId projectId);
}