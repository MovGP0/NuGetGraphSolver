namespace NuGetGraphSolver.Lib.Domain;

public sealed class PackageUniverse
{
    /// <summary>
    /// All packages included (top-level + transitive)
    /// </summary>
    public required Dictionary<string, List<PackageVersionNode>> CandidatesByPackage { get; init; }

    /// <summary>
    /// Top-level packages (the ones you asked for)
    /// </summary>
    public required HashSet<string> TopLevelPackages { get; init; }

    /// <summary>
    /// Resolved per-version, per-target-framework dependency set (post "nearest" selection)
    /// </summary>
    public required Dictionary<(string packageId, int versionIndex), List<DependencyInfo>> EffectiveDependencies { get; init; }
}