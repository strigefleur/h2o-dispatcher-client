using System.IO;
using Ardalis.GuardClauses;
using Felweed.Models.Enumerators;
using Felweed.Services;

namespace Felweed.Models;

public record CSharpSolution : Solution
{
    public override SolutionKind Kind => SolutionKind.CSharp;
    
    public override void Run(params string[] args)
    {
        if (Type == SolutionType.Library)
            throw new InvalidOperationException();
        
        var workingDirectory = GetWorkingDirectory(args);
        
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
            // TODO - обработка ошибок приклада
        }
    }

    private string GetWorkingDirectory(ICollection<string> prefixes)
    {
        var solutionDir = System.IO.Path.GetDirectoryName(Path);
        var srcPath = System.IO.Path.Combine(solutionDir, "src");
        var escapedName = Name.Replace("-", "");

        foreach (var prefix in prefixes)
        {
            var oldRunPath = System.IO.Path.Combine(srcPath, $"{prefix}.Services.{escapedName}");
            var newRunPath = System.IO.Path.Combine(srcPath, $"{prefix}.Services.{escapedName}.Web");
            
            if (Directory.Exists(newRunPath))
            {
                return newRunPath;
            }

            if (Directory.Exists(oldRunPath))
            {
                return oldRunPath;
            }
        }

        throw new InvalidOperationException();
    }
}