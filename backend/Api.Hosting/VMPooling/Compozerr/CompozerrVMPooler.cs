using Api.Abstractions;
using Api.Data.Repositories;
using Api.Hosting.VMPooling.Core;

namespace Api.Hosting.VMPooling.Compozerr;

public sealed class CompozerrVMPooler(
    ServerTierId serverTierId,
    LocationId locationId,
    IProjectRepository projectRepository,
    IVMPoolItemRepository vMPoolItemRepository) : BaseVMPooler(
        serverTierId,
        locationId,
        projectRepository,
        vMPoolItemRepository)
{
    internal override ProjectType ProjectType => ProjectType.Compozerr;
    internal override Uri DefaultRepoUri => new("https://github.com/compozerr/base");
    internal override string DefaultProjectName => "Compozerr pooled project";
}