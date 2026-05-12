using Api.Abstractions;
using Api.Data;
using MediatR;

namespace Api.Endpoints.Projects.Domains.ChangeService;

public sealed record ChangeDomainServiceRequest(string ServiceName, Protocol Protocol);

public static class ChangeDomainServiceRoute
{
    public const string Route = "{domainId:guid}/service";

    public static RouteHandlerBuilder AddChangeDomainServiceRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPatch(Route, ExecuteAsync);
    }

    public static Task<ChangeDomainServiceResponse> ExecuteAsync(
        ProjectId projectId, DomainId domainId,
        ChangeDomainServiceRequest request, IMediator mediator)
        => mediator.Send(new ChangeDomainServiceCommand(projectId, domainId, request.ServiceName, request.Protocol));
}
