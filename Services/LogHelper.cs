using System.IO;

namespace Felweed.Services;

public static class LogHelper
{
    public static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "Felweed", 
        "Logs"
    );

    public static readonly string LogFileName = $"{LogDirectory}/felweed-.txt";
}