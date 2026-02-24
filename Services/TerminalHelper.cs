using System.Diagnostics;

namespace Felweed.Services;

public static class TerminalHelper
{
    public static void Run(string directory, string command, string title, int cols = 240, int lines = 100)
    {
        var process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.WorkingDirectory = directory;
        process.StartInfo.Arguments = $"/C title {title} && mode con cols={cols} lines={lines} && {command}";
        process.StartInfo.UseShellExecute = false; // Required if redirecting output/input
        // process.StartInfo.RedirectStandardOutput = true; // Optional: to capture output
        // process.StartInfo.CreateNoWindow = true; // Optional: to hide the command window

        process.Start();
    }
}