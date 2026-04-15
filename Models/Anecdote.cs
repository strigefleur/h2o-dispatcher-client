namespace Felweed.Models;

public sealed record Anecdote
{
    public string? Id { get; init; }
    public DateTime? DateAdded { get; init; }
    public string? Content { get; init; }
    public string? Rating { get; init; }
}