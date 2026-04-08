namespace Felweed.Models.Digestion;

public sealed record CobwebState
{
    public DateTime BuildStartAt { get; private set; } = DateTime.UtcNow;
    public DateTime? BuildEndAt { get; private set; }
    public DateTime? LastUpdateFromWebhookAt { get; private set; }

    public List<CobwebProject> Projects { get; init; } = [];
    
    public CobwebProject? TryAddTag(long projectId, CobwebTag tag)
    {
        var project = Projects.FirstOrDefault(x => x.Id == projectId);
        if (project is null)
            return null;
        
        project.Tags.Add(tag);
        LastUpdateFromWebhookAt = DateTime.UtcNow;
        
        return project;
    }

    public void SetBuildEnd()
    {
        BuildEndAt = DateTime.UtcNow;
    }
}