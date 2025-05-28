using Core.MediatR;

namespace Api.Abstractions;

public sealed record DeployProjectCommand(
    ProjectId ProjectId,
    string CommitHash,
    string CommitMessage,
    string CommitAuthor,
    string CommitBranch,
    string CommitEmail,
    bool OverrideAuthorization = false) : ICommand<DeployProjectResponse>;