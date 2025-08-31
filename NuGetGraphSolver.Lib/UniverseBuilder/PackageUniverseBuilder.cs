using NuGet.Frameworks;
using NuGetGraphSolver.Lib.Domain;
using NuGetGraphSolver.Lib.Services;

namespace NuGetGraphSolver.Lib.UniverseBuilder;

public sealed class PackageUniverseBuilder
{
    private readonly IPackageMetadataProvider _provider;
    private readonly NuGetFramework _projectFramework;

    public PackageUniverseBuilder(IPackageMetadataProvider provider, NuGetFramework projectFramework)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _projectFramework = projectFramework ?? throw new ArgumentNullException(nameof(projectFramework));
    }

    public async Task<PackageUniverse> BuildAsync(
        IEnumerable<string> rootPackageIds,
        bool includePrerelease,
        int maxVersionsPerPackage,
        CancellationToken ct)
    {
        var roots = new HashSet<string>(rootPackageIds, StringComparer.OrdinalIgnoreCase);
        var candidatesByPackage = new Dictionary<string, List<PackageVersionNode>>(StringComparer.OrdinalIgnoreCase);
        var work = new Queue<string>(roots);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (work.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var pid = work.Dequeue();
            if (!seen.Add(pid)) continue;

            var versions = await _provider.GetPackageVersionsAsync(pid, includePrerelease, maxVersionsPerPackage, ct);
            candidatesByPackage[pid] = versions;

            // Add immediate dependencies (union across versions; we’ll prune later by TFM)
            foreach (var depId in versions.SelectMany(v => v.DependencyGroups)
                                          .SelectMany(g => g.Dependencies)
                                          .Select(d => d.PackageId))
            {
                if (!candidatesByPackage.ContainsKey(depId))
                    work.Enqueue(depId);
            }
        }

        // For each version, select effective dependency group for the project framework
        var effective = new Dictionary<(string p, int idx), List<DependencyInfo>>();

        foreach (var (pid, versions) in candidatesByPackage)
        {
            for (var i = 0; i < versions.Count; i++)
            {
                var v = versions[i];
                var group = FrameworkCompatibilityService.SelectNearestDependencyGroup(v.DependencyGroups, _projectFramework);
                effective[(pid, i)] = group?.Dependencies?.ToList() ?? [];
            }
        }

        return new PackageUniverse
        {
            CandidatesByPackage = candidatesByPackage,
            TopLevelPackages = roots,
            EffectiveDependencies = effective
        };
    }
}
