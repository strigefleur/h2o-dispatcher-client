using Felweed.Models.Enumerators;

namespace Felweed.ViewModels;

public sealed class LevelNodeVm
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required SolutionType SolutionType { get; init; }
    public required string Path { get; init; }
}