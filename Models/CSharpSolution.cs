using System.IO;
using Ardalis.GuardClauses;
using Felweed.Models.Enumerators;
using Felweed.Services;
using Serilog;

namespace Felweed.Models;

public record CSharpSolution : Solution
{
    public override SolutionKind Kind => SolutionKind.CSharp;
    
    public override void Run()
    {
        if (Type == SolutionType.Library)
            throw new InvalidOperationException();
        
        var workingDirectory = GetWorkingDirectory(Path);
        
        TerminalHelper.Run(workingDirectory, "dotnet run", Name);
    }
    
    public override void Pack()
    {
        if (Type == SolutionType.Service)
            throw new InvalidOperationException();
        
        var solutionDir = Guard.Against.Null(System.IO.Path.GetDirectoryName(Path));
        
        TerminalHelper.Run(solutionDir, @".\pack.cmd", $"{Name}: pack");
    }

    public override void InvalidateCache()
    {
        if (Type == SolutionType.Service)
            throw new InvalidOperationException();

        try
        {
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var nugetCachePath = System.IO.Path.Combine(userFolder, @".nuget\packages");
            var packagePath = System.IO.Path.Combine(nugetCachePath, PackageId);
            if (Directory.Exists(packagePath))
            {
                Directory.Delete(packagePath, true);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete package");
        }
    }

    private static string GetWorkingDirectory(string slnPath)
    {
        var solutionDir = System.IO.Path.GetDirectoryName(slnPath);
        var srcPath = System.IO.Path.Combine(solutionDir, "src");

        string? runnableDir = null;
        foreach (var directory in Directory.GetDirectories(srcPath))
        {
            if (File.Exists(System.IO.Path.Combine(directory, "Program.cs")))
            {
                runnableDir = directory;
            }
        }
        
        return runnableDir ?? throw new InvalidOperationException();
    }
}