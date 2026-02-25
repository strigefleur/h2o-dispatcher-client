using System.Diagnostics;

namespace Felweed.Services;

public static class TerminalHelper
{
    private static string? _currentSessionTitle = null;
    private static readonly Lock Lock = new();
    
    private const string SessionPrefix = "h2o_";

    public static void Run(string directory, string command, string title)
    {
        lock (Lock)
        {
            var sessionId = SessionPrefix + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            if (!IsOurTerminalWindowOpen())
            {
                // First run - open new Windows Terminal window
                StartNewTerminalWindow(directory, command, title, sessionId);
            }
            else
            {
                // Subsequent runs - open in new tab
                OpenInNewTab(directory, command, title);
            }
            
            _currentSessionTitle = sessionId;
        }
    }

    private static void StartNewTerminalWindow(string directory, string command, string title, string sessionId)
    {
        // Windows Terminal arguments:
        // --title: Set tab title
        // -d: Set starting directory
        // cmd /K: Run command and keep window open
        var escapedCommand = command.Replace("\"", "\\\"");
        var escapedDirectory = directory.Replace("\"", "\\\"");

        var startInfo = new ProcessStartInfo
        {
            FileName = "wt.exe",
            UseShellExecute = false,
            Arguments =
                $"--window 0 --title \"{sessionId}\" -d \"{escapedDirectory}\" cmd /K \"title {title} && {escapedCommand}\""
        };

        Process.Start(startInfo);

        // Give the terminal time to initialize
        Thread.Sleep(500);
    }

    private static void OpenInNewTab(string directory, string command, string title)
    {
        var escapedCommand = command.Replace("\"", "\\\"");
        var escapedDirectory = directory.Replace("\"", "\\\"");

        var startInfo = new ProcessStartInfo
        {
            FileName = "wt.exe",
            UseShellExecute = false,
            Arguments = $"-w 0 new-tab --title \"{title}\" -d \"{escapedDirectory}\" cmd /K \"{escapedCommand}\""
        };

        Process.Start(startInfo);
        Thread.Sleep(150);
    }
    
    private static bool IsOurTerminalWindowOpen()
    {
        if (string.IsNullOrEmpty(_currentSessionTitle))
            return false;

        return FindWindowsTerminalProcessWithTitle(_currentSessionTitle) != null;
    }

    private static Process? FindWindowsTerminalProcessWithTitle(string titleContains)
    {
        foreach (var process in Process.GetProcesses())
        {
            if (process.MainWindowHandle == IntPtr.Zero)
                continue;

            if ((process.ProcessName.Contains("WindowsTerminal") ||
                 process.ProcessName.Contains("Terminal") ||
                 process.ProcessName == "OpenConsole") && process.MainWindowTitle.Contains(titleContains))
            {
                return process;
            }
        }

        return null;
    }
}