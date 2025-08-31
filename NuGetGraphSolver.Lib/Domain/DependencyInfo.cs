using NuGet.Versioning;

namespace NuGetGraphSolver.Lib.Domain;

public sealed class DependencyInfo
{
    public required string PackageId { get; init; }
    public required VersionRange VersionRange { get; init; }
    public bool AutoReferenced { get; init; } = false;
    public bool DevelopmentDependency { get; init; } = false;
}