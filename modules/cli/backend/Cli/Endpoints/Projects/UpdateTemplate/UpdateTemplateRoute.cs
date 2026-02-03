using Api.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Projects.UpdateTemplate;

public sealed record UpdateTemplateRequest(
    string ProjectType,
    string? TemplateFile = null,
    string? StartCommand = null);

public static class UpdateTemplateRoute
{
    public const string Route = "{projectId:guid}/template";

    public static RouteHandlerBuilder AddUpdateTemplateRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPatch(Route, ExecuteAsync);
    }

    public static Task<UpdateTemplateResponse> ExecuteAsync(
        ProjectId projectId,
        UpdateTemplateRequest request,
        IMediator mediator)
        => mediator.Send(new UpdateTemplateCommand(
            projectId,
            request.ProjectType,
            request.TemplateFile,
            request.StartCommand));
}
