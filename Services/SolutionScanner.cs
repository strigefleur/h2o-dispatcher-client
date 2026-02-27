using System.Collections.Frozen;
using Felweed.Models;

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
    
    private readonly List<CSharpSolutionDependency> _csharpSolutionDeps;
    public IReadOnlyCollection<CSharpSolutionDependency> CsharpSolutionDeps => _csharpSolutionDeps.AsReadOnly();
    
    private readonly List<AngularSolutionDependency> _angularSolutionDeps;
    public IReadOnlyCollection<AngularSolutionDependency> AngularSolutionDeps => _angularSolutionDeps.AsReadOnly();

    public static async Task<SolutionScanner> ScanAsync(ICollection<string> scanPaths,
        ICollection<string>? cSharpAllowedPrefixes, CancellationToken cancellationToken = default)
    {
        var csharpSolutions = await ScanCSharpSolutionsAsync(scanPaths, cSharpAllowedPrefixes, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        // после сканирования всех проектов собираем список уникальных наименований, дополняя перечнем версий
        List<CSharpSolutionDependency> uniqueCsharpDeps = [];
        foreach (var csharpSolution in csharpSolutions)
        {
            foreach (var dep in csharpSolution.Dependencies)
            {
                var uniqueDep = uniqueCsharpDeps.SingleOrDefault(x => x.Name == dep.Name);
                if (uniqueDep == null)
                {
                    uniqueCsharpDeps.Add(dep with { });
                }
                else
                {
                    uniqueDep.AddVersion(dep.Versions.Single());
                }
            }
        }

        // на базе уникального набора зависимостей переназначаем исходный набор
        foreach (var csharpSolution in csharpSolutions)
        {
            List<CSharpSolutionDependency> unifiedDeps = [];
            foreach (var dep in csharpSolution.Dependencies)
            {
                var uniqueDep = uniqueCsharpDeps.Single(x => x.Name == dep.Name);

                uniqueDep.AddConsumer(new DependencyConsumer
                {
                    Version = dep.Versions.Single(),
                    Solution = csharpSolution
                });

                // в проекте может быть несколько упоминаний одной зависимости
                if (!unifiedDeps.Contains(uniqueDep))
                {
                    unifiedDeps.Add(uniqueDep);
                }
            }

            csharpSolution.ReplaceDependencies(unifiedDeps);
        }

        var angularSolutions = await ScanAngularSolutionsAsync(scanPaths, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        return new SolutionScanner(csharpSolutions, angularSolutions);
    }
}