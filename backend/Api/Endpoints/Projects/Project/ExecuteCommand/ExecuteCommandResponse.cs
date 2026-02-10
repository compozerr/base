namespace Api.Endpoints.Projects.Project.ExecuteCommand;

public sealed record ExecuteCommandResponse(int ExitCode, string Stdout, string Stderr);
