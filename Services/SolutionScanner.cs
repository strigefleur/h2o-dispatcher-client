using Felweed.Models;

namespace Felweed.Services;

public static partial class SolutionScanner
{
    private static List<CSharpSolution>? _csharpSolutions;
    public static IReadOnlyCollection<CSharpSolution> CsharpSolutions => _csharpSolutions.AsReadOnly();

    private static List<AngularSolution>? _angularSolutions;
    public static IReadOnlyCollection<AngularSolution> AngularSolutions => _angularSolutions.AsReadOnly();

    private static List<CSharpSolutionDependency>? _csharpSolutionDeps;
    public static IReadOnlyCollection<CSharpSolutionDependency> CsharpSolutionDeps => _csharpSolutionDeps.AsReadOnly();

    private static List<AngularSolutionDependency>? _angularSolutionDeps;
    public static IReadOnlyCollection<AngularSolutionDependency> AngularSolutionDeps => _angularSolutionDeps.AsReadOnly();

    public static async Task ScanAsync(
        ICollection<string> scanPaths,
        ICollection<string>? cSharpAllowedPrefixes,
        CancellationToken cancellationToken = default)
    {
        var csharpSolutions = await ScanCSharpSolutionsAsync(scanPaths, cSharpAllowedPrefixes, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        // Unify C# deps + attach consumers
        List<CSharpSolutionDependency> uniqueCsharpDeps = [];
        foreach (var s in csharpSolutions)
        {
            foreach (var dep in s.Dependencies)
            {
                var unique = uniqueCsharpDeps.SingleOrDefault(x => x.Name == dep.Name);
                if (unique is null) uniqueCsharpDeps.Add(dep with { });
                else unique.AddVersion(dep.Versions.Single());
            }
        }

        foreach (var s in csharpSolutions)
        {
            List<CSharpSolutionDependency> unified = [];
            foreach (var dep in s.Dependencies)
            {
                var unique = uniqueCsharpDeps.Single(x => x.Name == dep.Name);

                unique.AddConsumer(new DependencyConsumer
                {
                    Version = dep.Versions.Single(),
                    Solution = s
                });

                // в проекте может быть несколько упоминаний одной зависимости
                if (!unified.Contains(unique))
                    unified.Add(unique);
            }

            s.ReplaceDependencies(unified);
        }

        var angularSolutions = await ScanAngularSolutionsAsync(scanPaths, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        // Unify Angular deps + attach consumers (same approach)
        List<AngularSolutionDependency> uniqueAngularDeps = [];
        foreach (var s in angularSolutions)
        {
            foreach (var dep in s.Dependencies)
            {
                var unique = uniqueAngularDeps.SingleOrDefault(x => x.Name == dep.Name);
                if (unique is null) uniqueAngularDeps.Add(dep with { });
                else unique.AddVersion(dep.Versions.Single());
            }
        }

        foreach (var s in angularSolutions)
        {
            List<AngularSolutionDependency> unified = [];
            foreach (var dep in s.Dependencies)
            {
                var unique = uniqueAngularDeps.Single(x => x.Name == dep.Name);

                unique.AddConsumer(new DependencyConsumer
                {
                    Version = dep.Versions.Single(),
                    Solution = s
                });

                // в проекте может быть несколько упоминаний одной зависимости
                if (!unified.Contains(unique))
                    unified.Add(unique);
            }

            s.ReplaceDependencies(unified);
        }

        _csharpSolutions = csharpSolutions;
        _csharpSolutionDeps = uniqueCsharpDeps;
        _angularSolutions = angularSolutions;
        _angularSolutionDeps = uniqueAngularDeps;
    }
}