using FluentValidation;

namespace Api.Hosting.Endpoints.Projects.Config;

public sealed class ReportProjectConfigCommandValidator : AbstractValidator<ReportProjectConfigCommand>
{
    public ReportProjectConfigCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}
