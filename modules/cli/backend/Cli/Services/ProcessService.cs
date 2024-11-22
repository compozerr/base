using System.Diagnostics;

namespace Cli.Services;

public record ProcessResponse(bool Success, string Output);

public interface IProcessService
{
    Task<ProcessResponse> RunProcessAsync(string command);
    Task<ProcessResponse> RunProcessAsync(string command, Stream inputStream);
}

public class ProcessService : IProcessService
{
    private static Process CreateProcess(string command)
    {
        var fileName = command.Split(" ")[0];
        var arguments = command.Replace(fileName, "").Trim();

        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                RedirectStandardInput = false
            }
        };
    }

    private static async Task<ProcessResponse> WaitForExitAndGetResponseAsync(Process process)
    {
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();

            return new ProcessResponse(false, error);
        }

        var output = process.StandardOutput.ReadToEnd();
        return new ProcessResponse(true, output);
    }

    public Task<ProcessResponse> RunProcessAsync(string command)
    {
        var process = CreateProcess(command);
        process.Start();
        return WaitForExitAndGetResponseAsync(process);
    }

    public async Task<ProcessResponse> RunProcessAsync(string command, Stream inputStream)
    {
        var process = CreateProcess(command);
        process.StartInfo.RedirectStandardInput = true;

        process.Start();
        await inputStream.CopyToAsync(process.StandardInput.BaseStream);
        process.StandardInput.Close();

        return await WaitForExitAndGetResponseAsync(process);
    }
}