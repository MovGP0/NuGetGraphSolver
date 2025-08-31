using NuGetGraphSolver.Lib.Domain;

namespace NuGetGraphSolver.Lib.Services;

public interface IPackageMetadataProvider
{
    Task<List<PackageVersionNode>> GetPackageVersionsAsync(
        string packageId, bool includePrerelease, int maxVersions, CancellationToken cancellationToken);
}