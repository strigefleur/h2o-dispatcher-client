using NuGet.Versioning;

namespace Felweed.Services;

public static class VersionHelper
{
    public static string? IncPatchVersion(string version)
    {
        if (NuGetVersion.TryParse(version, out var v))
        {
            var newVersion = new NuGetVersion(
                v.Major, 
                v.Minor, 
                v.Patch + 1
            );
            
            return newVersion.ToString();
        }

        return null;
    }
}