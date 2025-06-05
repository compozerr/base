using MediatR;

namespace Api.Endpoints.Server.Tiers.Get;

public static class GetTiersRoute
{
	public const string Route = "tiers"; 

	public static RouteHandlerBuilder AddGetTiersRoute(this IEndpointRouteBuilder app)
	{
		return app.MapGet(Route, ExecuteAsync);
	}

	public static Task<GetTiersResponse> ExecuteAsync(
		GetTiersCommand command,
		IMediator mediator)
		=> mediator.Send(command);
}
