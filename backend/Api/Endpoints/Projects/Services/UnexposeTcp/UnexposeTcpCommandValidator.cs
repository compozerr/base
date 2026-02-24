using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Services.UnexposeTcp;

public sealed class UnexposeTcpCommandValidator : AbstractValidator<UnexposeTcpCommand>
{
    public UnexposeTcpCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);
    }
}
