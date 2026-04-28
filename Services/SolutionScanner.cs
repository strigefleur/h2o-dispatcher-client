using Felweed.Models;

namespace Felweed.Services;

public static partial class SolutionScanner
{
    private static List<CSharpSolution> _csharpSolutions = [];
    public static IReadOnlyCollection<CSharpSolution> CsharpSolutions => _csharpSolutions.AsReadOnly();

    private static List<AngularSolution> _angularSolutions = [];
    public static IReadOnlyCollection<AngularSolution> AngularSolutions => _angularSolutions.AsReadOnly();

    public static async Task ScanAsync(
        ICollection<string> scanPaths,
        ICollection<string>? cSharpAllowedPrefixes,
        CancellationToken cancellationToken = default)
    {
        _csharpSolutions = await ScanCSharpSolutionsAsync(scanPaths, cSharpAllowedPrefixes, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        _angularSolutions = await ScanAngularSolutionsAsync(scanPaths, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);
        
        MapDependencies(_csharpSolutions);
        MapDependencies(_angularSolutions);
    }

    private static void MapDependencies(IReadOnlyCollection<Solution> solutions)
    {
        foreach (var solution in solutions)
        {
            var relatableSolutions = solutions
                .Where(x => x.Id != solution.Id && x.ProducesDependencies.Count > 0)
                .Where(x => x.IsCorporate == true)
                .ToArray();
            
            foreach (var consumedDependency in solution.ConsumesDependencies.Where(x => x.IsCorporate()))
            {
                // среди других решений (без текущего), у которых есть производимые зависимости, ищем нашу
                foreach (var otherSolution in relatableSolutions)
                {
                    foreach (var producedDependency in otherSolution.ProducesDependencies)
                    {
                        if (producedDependency.Equals(consumedDependency.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            consumedDependency.Solution = otherSolution;
                            consumedDependency.Solution.AddConsumedBy(solution);
                        }
                    }
                }
            }
        }
    }
}