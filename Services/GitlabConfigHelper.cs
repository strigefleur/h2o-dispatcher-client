using System.IO;
using Felweed.Models.Enumerators;

namespace Felweed.Services;

public static class GitlabConfigHelper
{
    public static SolutionType? GetProjectType(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        // Read all lines and look for the one containing "file:"
        var fileLine = File.ReadLines(filePath)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("file:"));

        if (fileLine != null)
        {
            if (fileLine.Contains("ci-cdl/lib"))
                return SolutionType.Library;
            
            if (fileLine.Contains("ci-cdp/svc"))
                return SolutionType.Service;
        }

        return null;
    }
}