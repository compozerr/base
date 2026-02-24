using Api.Data.Extensions;
using FluentValidation;

namespace Api.Endpoints.Projects.Services.ExposeTcp;

public sealed class ExposeTcpCommandValidator : AbstractValidator<ExposeTcpCommand>
{
    public ExposeTcpCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x.ProjectId).MustBeOwnedByCallerAsync(scopeFactory);
    }
}
