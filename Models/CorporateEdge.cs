namespace Felweed.Models;

public sealed record CorporateEdge
{
    public required Solution From { get; init; }          // consumer
    public required Solution To { get; init; }            // producer
    public required string PackageName { get; init; }     // npm package or NuGet id
}