using Api.Abstractions;
using Api.Data;
using Stripe.Services;

namespace Api.Endpoints.Projects.Project.Get;

public sealed record GetProjectResponse(
    Guid Id,
    string Name,
    string RepoName,
    ProjectState State,
    List<string> Domains,
    string ServerTier,
    Money? ServerTierPrice,
    string? PrimaryDomain,
    ProjectType? Type);
