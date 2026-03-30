namespace Felweed.Models.Graph;

public sealed class Node
{
    public required Solution Solution { get; init; }
    public Guid Id => Solution.Id;

    // Можно кэшировать вычисляемые штуки
    public bool IsRootCandidate => Solution.IsRunnable; // сервис как корень для UI
}