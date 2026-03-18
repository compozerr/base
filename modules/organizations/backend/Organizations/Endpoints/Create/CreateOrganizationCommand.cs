using Core.MediatR;

namespace Organizations.Endpoints.Create;

public sealed record CreateOrganizationCommand(string Name, string Slug) : ICommand<CreateOrganizationResponse>;
public sealed record CreateOrganizationResponse(Guid Id, string Name, string Slug);
