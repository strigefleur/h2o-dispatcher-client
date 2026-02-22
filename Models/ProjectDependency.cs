using Felweed.Models.Enumerators;

namespace Felweed.Models;

public abstract record ProjectDependency
{
    protected ProjectDependency(string name, string version)
    {
        Name = name;
        Version = version;
        Type = DetectType();
    }
    
    public ProjectDependencyType Type { get; }
    public string Name { get; init; }
    public string Version { get; init; }
    
    public abstract string CorporateDepPrefix { get; }

    protected ProjectDependencyType DetectType() => Name.StartsWith(CorporateDepPrefix)
        ? ProjectDependencyType.Corporate
        : ProjectDependencyType.Public;
}