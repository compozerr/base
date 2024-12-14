using Auth.Abstractions;
using Core.MediatR;
using Github.Abstractions;

namespace Github.Endpoints.Installation.Upsert;

public sealed record UpsertInstallationCommand(
    UserId UserId,
    string AccessToken,
    string Scope) : ICommand<InstallationId>;