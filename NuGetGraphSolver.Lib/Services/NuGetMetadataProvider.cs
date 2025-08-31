using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGetGraphSolver.Lib.Domain;

namespace NuGetGraphSolver.Lib.Services;

public sealed class NuGetMetadataProvider : IPackageMetadataProvider
{
    private readonly List<SourceRepository> _repositories;
    private readonly ILogger _logger = NullLogger.Instance;

    public NuGetMetadataProvider(IEnumerable<string> v3Sources)
    {
        if (v3Sources == null) throw new ArgumentNullException(nameof(v3Sources));
        _repositories = v3Sources.Select(url => Repository.Factory.GetCoreV3(url)).ToList();
    }

    public async Task<List<PackageVersionNode>> GetPackageVersionsAsync(
        string packageId, bool includePrerelease, int maxVersions, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            throw new ArgumentException("PackageId must be provided", nameof(packageId));

        var cache = new SourceCacheContext();
        var all = new Dictionary<NuGetVersion, PackageVersionNode>(VersionComparer.VersionRelease);

        foreach (var repo in _repositories)
        {
            var metadata = await repo.GetResourceAsync<PackageMetadataResource>(cancellationToken);
            var entries = await metadata.GetMetadataAsync(
                packageId,
                includePrerelease,
                includeUnlisted: false,
                cache,
                _logger,
                cancellationToken);

            foreach (var m in entries)
            {
                var v = m.Identity.Version;
                if (all.ContainsKey(v)) continue;

                var dependencySets = m.DependencySets ?? [];
                var depGroups = new List<DependencyGroupInfo>();

                foreach (var group in dependencySets)
                {
                    var deps = group.Packages?.Select(d => new DependencyInfo
                    {
                        PackageId = d.Id,
                        VersionRange = d.VersionRange ?? VersionRange.All,
                        // AutoReferenced & DevelopmentDependency heuristics omitted (not available in the metadata model here)
                    }).ToList() ?? [];

                    depGroups.Add(new DependencyGroupInfo
                    {
                        TargetFrameworkMoniker = group.TargetFramework?.GetShortFolderName()
                                                 ?? NuGetFramework.AnyFramework.GetShortFolderName(),
                        Dependencies = deps
                    });
                }

                var node = new PackageVersionNode
                {
                    PackageId = m.Identity.Id,
                    Version = v,
                    SourceName = repo.PackageSource.Name ?? repo.PackageSource.Source,
                    Published = m.Published,
                    DependencyGroups = depGroups
                };

                all[v] = node;
            }
        }

        // Keep only the newest N versions (descending by version, then published)
        var result = all.Keys
                        .OrderByDescending(v => v, VersionComparer.VersionRelease)
                        .Take(maxVersions)
                        .Select(v => all[v])
                        .OrderBy(v => v.Version, VersionComparer.VersionRelease) // ascending for index semantics
                        .ToList();

        return result;
    }
}