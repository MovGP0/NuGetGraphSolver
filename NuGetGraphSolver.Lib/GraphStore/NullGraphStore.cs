using NuGetGraphSolver.Lib.Domain;

namespace NuGetGraphSolver.Lib.GraphStore;

public sealed class NullGraphStore : IGraphStore
{
    public Task UpsertPackageUniverseAsync(PackageUniverse universe, CancellationToken cancellationToken)
    {
        // no-op
        return Task.CompletedTask;
    }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}