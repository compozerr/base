using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Api.Hosting.VMPooling.Compozerr;
using Api.Hosting.VMPooling.N8n;

namespace Api.Hosting.VMPooling.Core;

public interface IVMPoolerFactory
{
    BaseVMPooler CreateVMPooler(VMPool vMPool);
}

public sealed class VMPoolerFactory(
    IProjectRepository projectRepository,
    IVMPoolItemRepository vMPoolItemRepository,
    IHostingApiFactory hostingApiFactory,
    IEnvironmentTargetResolver environmentTargetResolver,
    IServiceProvider serviceProvider) : IVMPoolerFactory
{
    public BaseVMPooler CreateVMPooler(VMPool vMPool)
    {
        return vMPool.ProjectType switch
        {
            ProjectType.Compozerr =>
                new CompozerrVMPooler(
                    vMPool,
                    projectRepository,
                    vMPoolItemRepository,
                    hostingApiFactory,
                    environmentTargetResolver,
                    serviceProvider),
            ProjectType.N8n =>
                new N8nVMPooler(
                    vMPool,
                    projectRepository,
                    vMPoolItemRepository,
                    hostingApiFactory,
                    environmentTargetResolver,
                    serviceProvider),
            _ => throw new NotSupportedException(
                $"VMPooler for project type '{vMPool.ProjectType}' is not supported.")
        };
    }
}
