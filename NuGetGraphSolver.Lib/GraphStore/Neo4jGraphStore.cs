using Neo4j.Driver;
using NuGet.Versioning;
using NuGetGraphSolver.Lib.Domain;

namespace NuGetGraphSolver.Lib.GraphStore;

public sealed class Neo4jGraphStore : IGraphStore
{
    private readonly IDriver _driver;

    public Neo4jGraphStore(string uri, string user, string password)
    {
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
    }

    public async Task UpsertPackageUniverseAsync(PackageUniverse universe, CancellationToken ct)
    {
        await using var session = _driver.AsyncSession();

        // Create Package and Version nodes
        foreach (var (packageId, versions) in universe.CandidatesByPackage)
        {
            foreach (var (node, index) in versions.Select((n, i) => (n, i)))
            {
                var props = new
                {
                    packageId = node.PackageId,
                    version = node.Version.ToNormalizedString(),
                    source = node.SourceName,
                    published = node.Published?.UtcDateTime
                };

                await session.RunAsync(@"
MERGE (p:Package {id: $packageId})
MERGE (v:PackageVersion {id: $packageId, version: $version})
ON CREATE SET v.source = $source, v.published = $published
MERGE (v)-[:VERSION_OF]->(p)
", props);
            }
        }

        // Create dependency relationships (range info on relationship)
        foreach (var kv in universe.EffectiveDependencies)
        {
            var (packageId, versionIndex) = kv.Key;
            var sourceNode = universe.CandidatesByPackage[packageId][versionIndex];
            var tfmDepList = kv.Value;

            foreach (var dep in tfmDepList)
            {
                var range = dep.VersionRange?.ToNormalizedString() ?? "";
                var (min, minInc, max, maxInc) = ExtractRange(dep.VersionRange);

                await session.RunAsync(@"
MERGE (p:Package {id: $pid})
MERGE (v:PackageVersion {id: $pid, version: $pver})
MERGE (dp:Package {id: $did})
MERGE (v)-[r:DEPENDS_ON {depId: $did, range: $range}]->(dp)
SET r.min = $min, r.minInclusive = $minInc, r.max = $max, r.maxInclusive = $maxInc
", new
                {
                    pid = sourceNode.PackageId,
                    pver = sourceNode.Version.ToNormalizedString(),
                    did = dep.PackageId,
                    range,
                    min = min?.ToNormalizedString(),
                    minInc,
                    max = max?.ToNormalizedString(),
                    maxInc
                });
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _driver.DisposeAsync();
    }

    private static (NuGetVersion? min, bool minInc, NuGetVersion? max, bool maxInc)
        ExtractRange(VersionRange? vr)
    {
        if (vr == null) return (null, false, null, false);
        return (vr.MinVersion, vr.IsMinInclusive, vr.MaxVersion, vr.IsMaxInclusive);
    }
}