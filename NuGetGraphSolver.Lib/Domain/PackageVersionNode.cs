using NuGet.Versioning;

namespace NuGetGraphSolver.Lib.Domain;

public sealed class PackageVersionNode
{
    public required string PackageId { get; init; }
    public required NuGetVersion Version { get; init; }
    public required string SourceName { get; init; }
    public DateTimeOffset? Published { get; init; }

    /// <summary>
    /// Dependency groups keyed by target framework moniker (TFM)
    /// </summary>
    public required List<DependencyGroupInfo> DependencyGroups { get; init; } = [];
}