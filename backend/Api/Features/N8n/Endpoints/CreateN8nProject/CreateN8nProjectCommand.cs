using Core.MediatR;

namespace Api.Features.N8n.Endpoints.CreateN8nProject;

public sealed record CreateN8nProjectCommand(
    string ProjectName,
    string LocationIso,
    string Tier) : ICommand<CreateN8nProjectResponse>;
