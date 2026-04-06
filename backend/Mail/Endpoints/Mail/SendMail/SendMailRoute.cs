using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Mail.Endpoints.Mail.SendMail;

public static class SendMailRoute
{
	public const string Route = "send"; 

	public static RouteHandlerBuilder AddSendMailRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPost(Route, ExecuteAsync);
	}

	public static Task ExecuteAsync(
		[FromBody] SendMailCommand command,
		IMediator mediator)
		=> mediator.Send(command);
}
