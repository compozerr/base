using Api.Abstractions;
using Api.Data;
using Api.Data.Events;
using Api.Data.Repositories;
using Api.Hosting.VMPooling.Core;

namespace Api.Hosting.VMPooling.N8n;

public sealed class N8nVMPooler(
    VMPool vMPool,
    IProjectRepository projectRepository,
    IVMPoolItemRepository vMPoolItemRepository) : BaseVMPooler(
        vMPool,
        projectRepository,
        vMPoolItemRepository)
{
    public static readonly Uri N8nTemplateRepoUrl = new("https://github.com/compozerr/n8n-template");

    internal override ProjectType ProjectType => ProjectType.N8n;
    internal override Uri DefaultRepoUri => N8nTemplateRepoUrl;

    internal override string DefaultProjectName => "N8n pooled project";

    internal override void QueueDomainEventsForNewInstance(Project project)
    {
        base.QueueDomainEventsForNewInstance(project);
        project.QueueDomainEvent(
            new N8nProjectCreatedEvent(
                project,
                OverrideAuthorization: true));
    }
}