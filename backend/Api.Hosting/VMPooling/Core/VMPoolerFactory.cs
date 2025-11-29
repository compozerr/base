using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.VMPooling.Compozerr;
using Api.Hosting.VMPooling.N8n;

namespace Api.Hosting.VMPooling.Core;

public interface IVMPoolerFactory
{
    BaseVMPooler CreateVMPooler(VMPool vMPool);
}

public sealed class VMPoolerFactory(
    IProjectRepository projectRepository,
    IVMPoolItemRepository vMPoolItemRepository) : IVMPoolerFactory
{
    public BaseVMPooler CreateVMPooler(VMPool vMPool)
    {
        var serverTierId = ServerTiers.GetById(new ServerTierId(vMPool.ServerTierId)).Id;
        var locationId = vMPool.Server!.LocationId!;
        var projectType = vMPool.ProjectType;

        return projectType switch
        {
            ProjectType.Compozerr =>
                new CompozerrVMPooler(
                    serverTierId,
                    locationId,
                    projectRepository,
                    vMPoolItemRepository),
            ProjectType.N8n =>
                new N8nVMPooler(
                    serverTierId,
                    locationId,
                    projectRepository,
                    vMPoolItemRepository),
            _ => throw new NotSupportedException(
                $"VMPooler for project type '{projectType}' is not supported.")
        };
    }
}