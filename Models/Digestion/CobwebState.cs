namespace Felweed.Models.Digestion;

public sealed record CobwebState
{
    public DateTime BuildStartAt { get; private set; } = DateTime.UtcNow;
    public DateTime? BuildEndAt { get; private set; }
    public DateTime? LastUpdateFromWebhookAt { get; private set; }

    public List<CobwebProject> Projects { get; init; } = [];
}