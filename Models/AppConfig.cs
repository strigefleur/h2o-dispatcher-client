namespace Felweed.Models;

public sealed record AppConfig
{
    public List<string> SolutionDirectories { get; set; } = [];
    public List<string> CSharpSolutionPrefixes { get; set; } = ["CFO", "DataHub"];
}