namespace Felweed.Models;

public sealed record DependencyConsumer
{
    public required string Version { get; init; }
    public required Solution Solution { get; init; }
}