using Cli.Endpoints.Modules.Add;

namespace Cli.Endpoints.Modules.ForkModule;

public sealed record ForkModuleResponse(
    ModuleDto[] ForkedModules,
    string SharedBranchName);
