namespace Felweed.Models.Graph;

public sealed record Edge(
    Guid FromId,                    // producer solution id
    Guid ToId,                      // consumer solution id
    string DependencyName,          // имя зависимости (package name)
    string? RequestedVersion        // версия из ConsumedDependency
);