using Core.MediatR;

namespace Cli.Endpoints.Modules.Add;

public sealed class AddModuleCommandHandler(
    ) : ICommandHandler<AddModuleCommand, AddModuleResponse>
{
    public async Task<AddModuleResponse> Handle(
        AddModuleCommand command,
        CancellationToken cancellationToken = default)
    {
        var modules = await ModulesGetter.GetModulesAsync(
            command.Organization,
            command.ModuleName,
            command.CommitHash,
            cancellationToken: cancellationToken);

        return new AddModuleResponse
        {
        };
    }
}
