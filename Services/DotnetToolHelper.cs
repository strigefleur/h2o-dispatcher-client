using System.Diagnostics;

namespace Felweed.Services;

public static class DotnetToolHelper
{
    public static async Task<bool> IsToolInstalled(string toolName, CancellationToken ct = default)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "tool list --global",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        // Check if the tool name appears in the first column of the tool list
        return output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.TrimStart().StartsWith(toolName, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<bool> RunDotnetCommand(string arguments, CancellationToken ct = default)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = false
            }
        };

        process.Start();
        await process.WaitForExitAsync(ct);

        return process.ExitCode == 0;
    }
}