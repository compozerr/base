using Api.Abstractions;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Auth.Services;
using Microsoft.AspNetCore.Http.HttpResults;
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
        ISubscriptionsService subscriptionsService)
    {
        var project = await projectRepository.GetProjectByIdWithDomainsAsync(projectId);

        if (project == null || project.UserId != currentUserAccessor.CurrentUserId)
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
            project.Type
        ));
    }

    private static async Task<Price?> GetServerTierPriceFromProjectSubscriptionAsync(
        ISubscriptionsService subscriptionsService,
        ProjectId projectId)
    {
        var subscription = await subscriptionsService.GetSubscriptionsForUserAsync().ContinueWith(t => t.Result
            .Where(s => s.ProjectId == projectId)
            .FirstOrDefault());

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
