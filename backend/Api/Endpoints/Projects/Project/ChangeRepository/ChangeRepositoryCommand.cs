using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.ChangeRepository;

public sealed record ChangeRepositoryCommand(
    ProjectId ProjectId,
    string RepoFullName) : ICommand<ChangeRepositoryResponse>;
