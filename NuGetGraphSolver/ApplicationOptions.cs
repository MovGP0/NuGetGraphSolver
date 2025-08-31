namespace NuGetGraphSolver;

public sealed class ApplicationOptions
{
    public required string[] PackageIds { get; init; } = [];
    public required string TargetFrameworkMoniker { get; init; } = "net8.0";
    public string[] PackageSources { get; init; } = ["https://api.nuget.org/v3/index.json"];
    public bool IncludePrerelease { get; init; } = false;
    public int MaxVersionsPerPackage { get; init; } = 20;
    public string? Neo4jUri { get; init; }
    public string? Neo4jUser { get; init; }
    public string? Neo4jPassword { get; init; }
}