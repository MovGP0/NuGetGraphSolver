using NuGetGraphSolver.Lib.Domain;

namespace NuGetGraphSolver.Lib.GraphStore;

public interface IGraphStore : IAsyncDisposable
{
    Task UpsertPackageUniverseAsync(PackageUniverse universe, CancellationToken cancellationToken);
}