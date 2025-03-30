using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed record AddDomainRequest(
    string Domain,
    string ServiceName);

public static class AddDomainRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddAddDomainRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<AddDomainResponse> ExecuteAsync(
        Guid projectId,
        AddDomainRequest request,
        IMediator mediator)
        => mediator.Send(
            new AddDomainCommand(
                ProjectId.Create(projectId),
                request.Domain,
                request.ServiceName));
}
