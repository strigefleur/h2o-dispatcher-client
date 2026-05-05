using System.Diagnostics;
using CliWrap;
using CliWrap.Buffered;
using Serilog;

namespace Felweed.Services;

public static class TerminalHelper
{
    private static readonly Lock Lock = new();
    
    private const string SessionPrefix = "h2o_";

    public static void Run(string directory, string command, string title)
    {
        lock (Lock)
        {
            if (!IsOurTerminalWindowOpen())
            {
                // First run - open new Windows Terminal window
                StartNewTerminalWindow(directory, command, title, SessionPrefix);
            }
            else
            {
                // Subsequent runs - open in new tab
                OpenInNewTab(directory, command, title, SessionPrefix);
            }
        }
    }

    private static void StartNewTerminalWindow(string directory, string command, string title, string sessionPrefix)
    {
        var escapedCommand = command.Replace("\"", "\\\"");
        var escapedDirectory = directory.Replace("\"", "\\\"");
        var escapedTitle = title.Replace("-", "_");
        
        var compoundTitle = $"{sessionPrefix}{escapedTitle}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "wt.exe",
            UseShellExecute = false,
            Arguments =
                $"-w 0 --suppressApplicationTitle --title \"{compoundTitle}\" -d \"{escapedDirectory}\" cmd /K \"{escapedCommand}\""
        };

        Process.Start(startInfo);

        // Give the terminal time to initialize
        Thread.Sleep(500);
    }

    private static void OpenInNewTab(string directory, string command, string title, string sessionPrefix)
    {
        var escapedCommand = command.Replace("\"", "\\\"");
        var escapedDirectory = directory.Replace("\"", "\\\"");
        var escapedTitle = title.Replace("-", "_");
        
        var compoundTitle = $"{sessionPrefix}{escapedTitle}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "wt.exe",
            UseShellExecute = false,
            Arguments = $"-w 0 new-tab --suppressApplicationTitle --title \"{compoundTitle}\" -d \"{escapedDirectory}\" cmd /K \"{escapedCommand}\""
        };

        Process.Start(startInfo);
        Thread.Sleep(150);
    }
    
    private static bool IsOurTerminalWindowOpen()
    {
        return FindWindowsTerminalProcessWithTitle(SessionPrefix) != null;
    }

    private static Process? FindWindowsTerminalProcessWithTitle(string titleStartsWith)
    {
        foreach (var process in Process.GetProcesses())
        {
            if (process.MainWindowHandle == IntPtr.Zero) continue;
        
            // Check for Windows Terminal processes
            if (process.ProcessName.Contains("WindowsTerminal") || process.ProcessName.Contains("Terminal"))
            {
                // Check if title contains our session ID
                if (process.MainWindowTitle.StartsWith(titleStartsWith))
                    return process;
            }
        }
        return null;
    }
    
    public static async Task<bool> RunCmd(string cmd, string args, string workDir, CancellationToken ct)
    {
        try
        {
            var info = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {cmd} {args}",
                WorkingDirectory = workDir,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var proc = Process.Start(info);
            if (proc == null)
                return false;

            await proc.WaitForExitAsync(ct);
            if (proc.ExitCode != 0)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to run cmd");
            return false;
        }
    }
    
    public static async Task<bool> NugetClearCacheAsync(string solutionDir, CancellationToken ct = default)
    {
        var stageResult = await Cli.Wrap("dotnet")
            .WithArguments("nuget locals http-cache --clear")
            .WithWorkingDirectory(solutionDir)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(ct);

        if (!stageResult.IsSuccess)
        {
            Log.Error(stageResult.StandardError);
            return false;
        }

        return true;
    }
    
    public static async Task<bool> DotnetRestoreAsync(string solutionDir, CancellationToken ct = default)
    {
        var stageResult = await Cli.Wrap("dotnet")
            .WithArguments("restore")
            .WithWorkingDirectory(solutionDir)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(ct);

        if (!stageResult.IsSuccess)
        {
            Log.Error(stageResult.StandardError);
            return false;
        }

        return true;
    }

    public static async Task<bool> DotnetPackageUpdateAsync(string solutionDir, string packageId,
        CancellationToken ct = default)
    {
        var stageResult = await Cli.Wrap("dotnet")
            .WithArguments($"package update {packageId}")
            .WithWorkingDirectory(solutionDir)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(ct);

        if (!stageResult.IsSuccess)
        {
            Log.Error(stageResult.StandardError);
            return false;
        }

        return true;
    }
}