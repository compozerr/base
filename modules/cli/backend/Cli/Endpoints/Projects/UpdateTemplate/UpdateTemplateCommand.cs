using Api.Abstractions;
using Core.MediatR;

namespace Cli.Endpoints.Projects.UpdateTemplate;

public sealed record UpdateTemplateCommand(
    ProjectId ProjectId,
    string ProjectType,
    string? TemplateFile = null,
    string? StartCommand = null) : ICommand<UpdateTemplateResponse>;
