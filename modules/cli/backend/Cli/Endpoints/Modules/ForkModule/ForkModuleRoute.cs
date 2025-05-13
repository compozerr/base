using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Cli.Endpoints.Modules.Add;
using Api.Abstractions;

namespace Cli.Endpoints.Modules.ForkModule;

public sealed record ForkModuleRequest(
	ModuleDto[] ModulesToFork,
	string ProjectId) : IRequest<ForkModuleResponse>;

public static class ForkModuleRoute
{
	public const string Route = "fork-modules"; // Set your route path here

	public static RouteHandlerBuilder AddForkModuleRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPost(Route, ExecuteAsync);
	}

	public static Task<ForkModuleResponse> ExecuteAsync(
		ForkModuleRequest request,
		IMediator mediator)
		=> mediator.Send(
			new ForkModuleCommand(
				request.ModulesToFork,
				ProjectId.Parse(request.ProjectId)));
}
