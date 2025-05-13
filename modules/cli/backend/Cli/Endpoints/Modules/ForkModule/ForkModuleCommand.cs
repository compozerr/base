using Api.Abstractions;
using Cli.Endpoints.Modules.Add;
using Core.MediatR;

namespace Cli.Endpoints.Modules.ForkModule;

public sealed record ForkModuleCommand(
	ModuleDto[] ModulesToFork,
	ProjectId ProjectId) : ICommand<ForkModuleResponse>;
