using Felweed.Models.Enumerators;

namespace Felweed.Models;

public abstract record Solution
{
    public Guid Id { get; } = Guid.NewGuid();
    public abstract SolutionKind Kind { get; }
    public required string Path { get; init; }
    public required string Name { get; init; }
    public required string? ChangelogVersionNumber { get; init; }
    public required SolutionType? Type { get; init; }
    public required DateTime? LatestSyncDate { get; init; }

    public bool IsRunnable => Type == SolutionType.Service;

    public bool? IsCorporate { get; init; }
    
    private readonly HashSet<string> _producesDependencies = [];
    private readonly HashSet<ConsumedDependency> _consumesDependencies = [];
    private readonly HashSet<Solution> _consumedBy = [];
    
    public IReadOnlyCollection<string> ProducesDependencies => _producesDependencies.ToArray().AsReadOnly();

    public IReadOnlyCollection<ConsumedDependency> ConsumesDependencies =>
        _consumesDependencies.ToArray().AsReadOnly();
    
    public IReadOnlyCollection<ConsumedDependency> ConsumesCorpDependencies =>
        _consumesDependencies.Where(x => x.IsCorporate).ToArray().AsReadOnly();
    
    public IReadOnlyCollection<Solution> ConsumedBy => _consumedBy.ToArray().AsReadOnly();

    public void AddProducedDependencies(params string[] dependencies)
    {
        if (dependencies.Length == 0)
            return;
        
        if (Type == SolutionType.Service)
            throw new InvalidOperationException();

        foreach (var dependency in dependencies)
        {
            _producesDependencies.Add(dependency);
        }
    }
    
    public void AddConsumedDependencies(params ConsumedDependency[] dependencies)
    {
        foreach (var dependency in dependencies)
        {
            _consumesDependencies.Add(dependency);
        }
    }

    public void AddConsumedBy(Solution solution)
    {
        _consumedBy.Add(solution);
    }

    public abstract void Run(params string[] args);
}