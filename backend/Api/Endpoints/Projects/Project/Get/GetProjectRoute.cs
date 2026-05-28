using Api.Abstractions;
using Api.Data;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Api.Data.Services;
using Api.Hosting.Services;
using Auth.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Organizations.Services;
using Stripe.Endpoints.Subscriptions.GetUserSubscriptions;
using Stripe.Services;

namespace Api.Endpoints.Projects.Project.Get;

public static class GetProjectRoute
{
    public const string Route = "{projectId:guid}";

    public static RouteHandlerBuilder AddGetProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok<GetProjectResponse>, NotFound>> ExecuteAsync(
        ProjectId projectId,
        ICurrentUserAccessor currentUserAccessor,
        IProjectRepository projectRepository,
        IOrganizationAuthorizationService orgAuthService,
        ISubscriptionsService subscriptionsService,
        IServerVisibilityFilter serverVisibilityFilter)
    {
        var project = await projectRepository.GetProjectByIdWithDomainsAsync(projectId);
        if (project is null)
            return TypedResults.NotFound();

        var userId = currentUserAccessor.CurrentUserId!;
        var hasAccess = project.OrganizationId is not null
            ? await orgAuthService.CanAccessProjectAsync(project.OrganizationId, userId)
            : project.UserId == userId;

        if (!hasAccess)
        {
            return TypedResults.NotFound();
        }

        var projectDomains = project.Domains?.Where(x => x.DeletedAtUtc == null).ToList();

        var serverTierPrice = await GetServerTierPriceFromProjectSubscriptionAsync(
            subscriptionsService,
            projectId);

        return TypedResults.Ok(new GetProjectResponse(
            project.Id.Value,
            project.Name,
            RepoUri.Parse(project.RepoUri).RepoName,
            project.State,
            [.. projectDomains?.Select(x => x.GetValue) ?? []],
            project.ServerTierId.Value,
            serverTierPrice,
            projectDomains?.GetPrimary()?.GetValue,
            project.Type,
            // Project-level Server summary reflects the production env's server
            // (canonical "project server" for the project card / header).
            // GetProjectByIdWithDomainsAsync eager-loads env.Server, so this is
            // resolved purely from the navigation graph — no extra DB hit.
            // Mask the resolved server when it is not visible to the caller
            // (e.g. an AdminOnly server on a project the caller can access but
            // isn't a platform admin for). Graceful degradation — Server = null.
            Server: ResolveProjectServerSummary(project, serverVisibilityFilter),
            DisplayState: project.GetDisplayState()
        ));
    }

    private static AllocatedServerSummary? ResolveProjectServerSummary(
        Data.Project project,
        IServerVisibilityFilter serverVisibilityFilter)
    {
        // Pick the env-level server: prefer Production env's server (the
        // canonical "project server"), falling back to any env that has one
        // when the production env was deleted or hasn't been allocated yet.
        var prodEnvServer = project.ProjectEnvironments?
            .FirstOrDefault(e => e.Type == EnvironmentType.Production)?
            .Server;

        var server = prodEnvServer
            ?? project.ProjectEnvironments?
                .FirstOrDefault(e => e.Server is not null)?
                .Server;

        if (server is null) return null;

        var visibleServer = serverVisibilityFilter.ApplyForCurrentUser(new[] { server }).FirstOrDefault();
        if (visibleServer is null) return null;

        return new AllocatedServerSummary(
            visibleServer.Id,
            visibleServer.HostName,
            visibleServer.Location?.IsoCountryCode);
    }

    private static async Task<Price?> GetServerTierPriceFromProjectSubscriptionAsync(
        ISubscriptionsService subscriptionsService,
        ProjectId projectId)
    {
        var subscriptions = await subscriptionsService.GetSubscriptionsForUserAsync();
        var subscription = subscriptions
            .Where(s => s.ProjectId == projectId)
            .FirstOrDefault();

        return GetServerTierPriceFromSubscriptionDto(subscription);
    }

    private static Price? GetServerTierPriceFromSubscriptionDto(SubscriptionDto? subscription)
    {
        if (subscription is not { Amount: { } } nonNullSubscription)
            return null;

        return new Price(
            nonNullSubscription.Amount.Amount,
            nonNullSubscription.Amount.Currency,
            nonNullSubscription.OriginalAmount?.Amount);
    }
}
