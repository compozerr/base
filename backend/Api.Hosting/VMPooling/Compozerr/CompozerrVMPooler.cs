using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Api.Hosting.VMPooling.Core;

namespace Api.Hosting.VMPooling.Compozerr;

public sealed class CompozerrVMPooler(
    VMPool vMPool,
    IProjectRepository projectRepository,
    IVMPoolItemRepository vMPoolItemRepository,
    IHostingApiFactory hostingApiFactory,
    IEnvironmentTargetResolver environmentTargetResolver,
    IServiceProvider serviceProvider) : BaseVMPooler(
        vMPool,
        projectRepository,
        vMPoolItemRepository,
        hostingApiFactory,
        environmentTargetResolver,
        serviceProvider)
{
    internal override ProjectType ProjectType => ProjectType.Compozerr;
    internal override Uri DefaultRepoUri => new("https://github.com/compozerr/base");
    internal override string DefaultProjectName => "Compozerr pooled project";
}
