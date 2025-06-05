using Core.MediatR;

namespace Api.Endpoints.Server.Tiers.Get;

public sealed record GetTiersCommand() : ICommand<GetTiersResponse>;
