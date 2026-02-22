using System.IO;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Xml.Linq;
using Ardalis.GuardClauses;
using Felweed.Models;
using Felweed.Models.Enumerators;

namespace Felweed.Services;

public partial class SolutionScanner
{
    private SolutionScanner(List<CSharpSolution> csharpSolutions, List<AngularSolution> angularSolutions)
    {
        _csharpSolutions = csharpSolutions;
        _angularSolutions = angularSolutions;
    }
    
    private readonly List<CSharpSolution> _csharpSolutions;
    public IReadOnlyCollection<CSharpSolution> CsharpSolutions => _csharpSolutions.AsReadOnly();
    
    private readonly List<AngularSolution> _angularSolutions;
    public IReadOnlyCollection<AngularSolution> AngularSolutions => _angularSolutions.AsReadOnly();

    public static async Task<SolutionScanner> ScanAsync(string[] scanPaths, CancellationToken cancellationToken = default)
    {
        var csharpSolutions = await ScanCSharpSolutionsAsync(scanPaths, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        var angularSolutions = await ScanAngularSolutionsAsync(scanPaths, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);
        
        return new SolutionScanner(csharpSolutions, angularSolutions);
    }
    
    #region C# Scanning
    
    private static async IAsyncEnumerable<CSharpSolution> ScanCSharpSolutionsAsync(
        IEnumerable<string> directories,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<string>();
    
        // Producer: find all .sln files
        _ = Task.Run(async () =>
        {
            try
            {
                foreach (var dir in directories)
                {
                    foreach (var file in EnumerateFilesWithExclusions(dir, "*.sln"))
                    {
                        await channel.Writer.WriteAsync(file, ct);
                    }
                }
            }
            finally { channel.Writer.Complete(); }
        }, ct);

        // Consumer: parse and yield
        await foreach (var file in channel.Reader.ReadAllAsync(ct))
        {
            yield return ParseCSharpSolution(file);
        }
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
    
    private static async IAsyncEnumerable<AngularSolution> ScanAngularSolutionsAsync(
        IEnumerable<string> directories,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<string>();
    
        // Producer: find all angular.json files
        _ = Task.Run(async () =>
        {
            try
            {
                foreach (var dir in directories)
                {
                    foreach (var file in EnumerateFilesWithExclusions(dir, "angular.json"))
                    {
                        await channel.Writer.WriteAsync(Guard.Against.Null(Path.GetDirectoryName(file)), ct);
                    }
                }
            }
            finally { channel.Writer.Complete(); }
        }, ct);

        // Consumer: parse and yield
        await foreach (var path in channel.Reader.ReadAllAsync(ct))
        {
            yield return ParseAngularSolution(path);
        }
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

    public static IEnumerable<string> EnumerateFilesWithExclusions(string rootPath, string searchPattern = "*")
    {
        return new FileSystemEnumerable<string>(
            rootPath,
            (ref FileSystemEntry entry) => entry.ToFullPath(),
            ScanOptions)
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory &&
                                                                    FileSystemName.MatchesWin32Expression(searchPattern,
                                                                        entry.FileName),
            // Don't recurse into excluded directories
            ShouldRecursePredicate = (ref FileSystemEntry entry) =>
            {
                foreach (var excluded in DirectoriesToSkip)
                {
                    if (entry.FileName.Equals(excluded.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            }
        };
    }

    private static readonly HashSet<string> DirectoriesToSkip = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules",
        "bin",
        "obj",
        ".git",
        ".vs",
        "packages"
    };

    private static readonly EnumerationOptions ScanOptions = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        // Skips symbolic links to avoid loops
        AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System | FileAttributes.Hidden
    };
}