namespace NuGetGraphSolver.Lib.Domain;

public sealed class DependencyGroupInfo
{
    public required string TargetFrameworkMoniker { get; init; }
    public required List<DependencyInfo> Dependencies { get; init; } = [];
}