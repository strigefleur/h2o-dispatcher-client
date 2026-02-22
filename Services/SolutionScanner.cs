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

    public static async Task<SolutionScanner> ScanAsync(string[] scanPaths, CancellationToken cancellationToken = default)
    {
        var csharpSolutions = await ScanCSharpSolutionsAsync(scanPaths, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        var angularSolutions = await ScanAngularSolutionsAsync(scanPaths, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);
        
        return new SolutionScanner(csharpSolutions, angularSolutions);
    }
}