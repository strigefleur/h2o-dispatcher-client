using Felweed.Services;

namespace Felweed.Models;

public sealed record NugetFeedConfig
{
    public required string? ConfigPath { get; init; }
    public required string? Name { get; init; }
    public required string? Url { get; init; }

    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return false;

        if (UrlHelper.GetSafeUrl(Url) == null)
            return false;

        if (string.IsNullOrWhiteSpace(ConfigPath))
            return false;

        return true;
    }
}