using System.Globalization;
using Felweed.Models.Digestion;
using Felweed.Models.Enumerators;

namespace Felweed.Models;

public abstract record Solution
{
    public Guid Id { get; } = Guid.NewGuid();
    public abstract SolutionKind Kind { get; }
    public required string Path { get; init; }
    public required string Name { get; init; }
    public required string PackageId { get; init; }
    public string? TagVersionNumber { get; private set; }
    public required string? GitOriginUrl { get; init; }
    public required SolutionType? Type { get; init; }
    public required DateTime? LatestSyncDate { get; init; }
    
    public CobwebProject? CobwebProject { get; private set; }

    public bool IsRunnable => Type == SolutionType.Service;
    public bool IsPackable => Type == SolutionType.Library;

    public bool IsOutdated => CobwebProject is not null && CobwebProject.Tags.Count > 0 &&
                                       CobwebProject.Tags.Any(x => string.Compare(x.Name, TagVersionNumber,
                                           CultureInfo.InvariantCulture, CompareOptions.NumericOrdering) > 0);

    public string? PipelineUrl => CobwebProject == null ? null : $"{CobwebProject?.WebUrl}/-/pipelines";

    public bool? IsCorporate { get; init; }
    
    private readonly HashSet<string> _producesDependencies = [];
    private readonly HashSet<ConsumedDependency> _consumesDependencies = [];
    private readonly HashSet<Solution> _consumedBy = [];
    
    public IReadOnlyCollection<string> ProducesDependencies => _producesDependencies.ToArray().AsReadOnly();

    public IReadOnlyCollection<ConsumedDependency> ConsumesDependencies =>
        _consumesDependencies.ToArray().AsReadOnly();
    
    public IReadOnlyCollection<ConsumedDependency> ConsumesCorpDependencies =>
        _consumesDependencies.Where(x => x.IsCorporate()).ToArray().AsReadOnly();
    
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

    public void BindToCobwebProject(CobwebProject? cobwebProject)
    {
        if (CobwebProject != null)
            throw new InvalidOperationException();
        
        CobwebProject = cobwebProject;
    }

    public abstract void Run(params string[] args);
    public abstract void Pack();
    public abstract void InvalidateCache();
    
    public void UpdateTagVersionNumber(string? tagVersion)
    {
        TagVersionNumber = tagVersion;
    }
}