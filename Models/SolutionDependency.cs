using Felweed.Models.Enumerators;

namespace Felweed.Models;

public abstract record SolutionDependency
{
    protected SolutionDependency(string name, string version)
    {
        Name = name;
        _versions.Add(version);
        Type = DetectType();
    }

    protected SolutionDependency(SolutionDependency other)
    {
        Type = other.Type;
        Name = other.Name;

        _versions = [];
        foreach (var version in other.Versions)
        {
            _versions.Add(version);
        }
        
        _consumers = [];
    }
    
    public SolutionDependencyType Type { get; }
    public string Name { get; init; }
    
    private readonly HashSet<string> _versions = [];
    public IReadOnlyCollection<string> Versions => _versions;
    
    private readonly HashSet<DependencyConsumer> _consumers = [];
    public IReadOnlyCollection<DependencyConsumer> Consumers => _consumers;
    
    public abstract string CorporateDepPrefix { get; }

    protected SolutionDependencyType DetectType() => Name.StartsWith(CorporateDepPrefix)
        ? SolutionDependencyType.Corporate
        : SolutionDependencyType.Public;
    
    public void AddVersion(string version) => _versions.Add(version);
    
    public void AddConsumer(DependencyConsumer consumer) => _consumers.Add(consumer);
}