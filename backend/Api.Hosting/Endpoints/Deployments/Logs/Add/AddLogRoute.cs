using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System.Text.Json.Serialization;
namespace Api.Hosting.Endpoints.Deployments.Logs.Add;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LogLevel
{
    Info,
    Warning,
    Error,
    Success
}

public sealed record AddLogRequest(string Log, LogLevel Level);

public static class AddLogRoute
{
    public const string Route = "{deploymentId:guid}/logs";

    public static RouteHandlerBuilder AddAddLogRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task ExecuteAsync(
        Guid deploymentId,
        AddLogRequest request,
        IMediator mediator)
    {
        return Task.CompletedTask;
    }
}
