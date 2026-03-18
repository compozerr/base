using Core.MediatR;
using Organizations.Abstractions;

namespace Organizations.Endpoints.Update;

public sealed record UpdateOrganizationCommand(OrganizationId OrganizationId, string Name, string? AvatarUrl) : ICommand<UpdateOrganizationResponse>;
public sealed record UpdateOrganizationResponse(Guid Id, string Name);
