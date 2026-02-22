using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Ardalis.GuardClauses;
using Felweed.Models;
using Felweed.Models.Enumerators;

namespace Felweed.Services;

public partial class SolutionScanner
{
    private SolutionScanner(params string[] scanPaths)
    {
        foreach (var root in scanPaths.Where(Directory.Exists))
        {
            // 1. Detect C# Solutions (.sln files)
            _csharpSolutionPaths.AddRange(Directory.EnumerateFiles(root, "*.sln", ScanOptions));
    
            // 2. Detect Angular Solutions (Folders with package.json containing @angular/core)
            _angularSolutionPaths.AddRange(Directory.EnumerateFiles(root, "package.json", ScanOptions)
                .Where(file => File.ReadAllText(file).Contains("\"@angular/core\""))
                .Select(x => Guard.Against.Null(Path.GetDirectoryName(x))));
        }
    }

    private readonly List<string> _csharpSolutionPaths = [];
    private readonly List<string> _angularSolutionPaths = [];
    
    private List<CSharpSolution> _csharpSolutions = [];
    public IReadOnlyCollection<CSharpSolution> CsharpSolutions => _csharpSolutions.AsReadOnly();
    
    private List<AngularSolution> _angularSolutions = [];
    public IReadOnlyCollection<AngularSolution> AngularSolutions => _angularSolutions.AsReadOnly();

    public async Task<SolutionScanner> ScanAsync(string[] scanPaths, CancellationToken cancellationToken = default)
    {
        var scanner = new SolutionScanner(scanPaths);
        
        _csharpSolutions = await Task.Run(() => ScanCSharpSolutions(_csharpSolutionPaths), cancellationToken);
        _angularSolutions = await Task.Run(() => ScanAngularSolutions(_angularSolutionPaths), cancellationToken);
        
        return scanner;
    }
    
    #region C# Scanning

    private static List<CSharpSolution> ScanCSharpSolutions(IEnumerable<string> directories)
    {
        return directories
            .SelectMany(d => Directory.EnumerateFiles(d, "*.sln", ScanOptions))
            .Select(ParseCSharpSolution)
            .ToList();
    }

    private static CSharpSolution ParseCSharpSolution(string slnPath)
    {
        var dependencies = new List<CSharpSolutionDependency>();
        var slnDir = Path.GetDirectoryName(slnPath);

        // Find all .csproj files referenced in solution
        var projects = GetProjectsFromSolution(slnPath, slnDir);

        foreach (var csproj in projects.Where(File.Exists))
        {
            dependencies.AddRange(ParseCsprojDependencies(csproj));
        }

        var solution = new CSharpSolution
        {
            Name = "",
            Path = slnPath
        };
        solution.AddDependencyRange([.. dependencies.Distinct().OrderBy(d => d.Name)]);

        return solution;
    }

    private static List<string> GetProjectsFromSolution(string slnPath, string slnDir)
    {
        var content = File.ReadAllText(slnPath);
        return ProjectRegex()
            .Matches(content)
            .Select(m => Path.GetFullPath(Path.Combine(slnDir, m.Groups[1].Value)))
            .Where(p => p.EndsWith(".csproj"))
            .ToList();
    }

    private static List<CSharpSolutionDependency> ParseCsprojDependencies(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            return doc.Descendants()
                .Where(e => e.Name.LocalName == "PackageReference")
                .Select(e =>
                {
                    var name = e.Attribute("Include")?.Value ?? "Unknown";
                    var version = e.Attribute("Version")?.Value ?? e.Element(ns + "Version")?.Value ?? "*";

                    return new CSharpSolutionDependency(name, version);
                })
                .ToList();
        }
        catch { return []; }
    }

    [GeneratedRegex(@"Project\([^)]+\)\s*=\s*""[^""]*""\s*,\s*""([^""]+)""")]
    private static partial Regex ProjectRegex();

    #endregion C# Scanning
    
    #region Angular Scanning
    
    private static List<AngularSolution> ScanAngularSolutions(IEnumerable<string> directories)
    {
        return directories
            .SelectMany(d => Directory.EnumerateFiles(d, "angular.json", ScanOptions))
            .Select(f => Path.GetDirectoryName(f)!)
            .Select(ParseAngularSolution)
            .ToList();
    }
    
    private static AngularSolution ParseAngularSolution(string angularDir)
    {
        var packageJson = Path.Combine(angularDir, "package.json");
        var dependencies = File.Exists(packageJson) 
            ? ParsePackageJson(packageJson) 
            : [];

        var solution = new AngularSolution
        {
            Name = "",
            Path = angularDir
        };
        solution.AddDependencyRange(dependencies.ToArray());

        return solution;
    }
    
    private static List<AngularSolutionDependency> ParsePackageJson(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var deps = new List<AngularSolutionDependency>();

            deps.AddRange(ExtractDeps(root, "dependencies", AngularDependencyType.Runtime));
            deps.AddRange(ExtractDeps(root, "devDependencies", AngularDependencyType.Dev));
            deps.AddRange(ExtractDeps(root, "peerDependencies", AngularDependencyType.Peer));

            return [.. deps.OrderBy(d => d.Name)];
        }
        catch { return []; }
    }
    
    private static IEnumerable<AngularSolutionDependency> ExtractDeps(JsonElement root, string section, AngularDependencyType type)
    {
        if (!root.TryGetProperty(section, out var element)) 
            yield break;

        foreach (var prop in element.EnumerateObject())
            yield return new AngularSolutionDependency(prop.Name, prop.Value.GetString() ?? "*", type);
    }
    
    #endregion Angular Scanning

    private static readonly EnumerationOptions ScanOptions = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        // Skips symbolic links to avoid loops
        AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System | FileAttributes.Hidden
    };
}