namespace NuGetGraphSolver.Lib;

public static class Config
{
    /// <summary>
    /// Top-level packages you require simultaneously
    /// </summary>
    public static readonly string[] PackageIds =
    [
        "Serilog", "Serilog.Sinks.Console"
    ];

    /// <summary>
    /// NuGet v3 service index endpoints (can be private feeds too)
    /// </summary>
    public static readonly string[] PackageSources =
    [
        "https://api.nuget.org/v3/index.json"
    ];

    /// <summary>
    /// Target framework moniker (TFM) of your project. Example: "net8.0"
    /// </summary>
    public const string TargetFrameworkMoniker = "net8.0";

    /// <summary>
    /// Whether to allow pre-release versions in the universe
    /// </summary>
    public const bool IncludePrerelease = false;

    /// <summary>
    /// Keep search bounded: take only the newest N versions of each package
    /// </summary>
    public const int MaxVersionsPerPackage = 20;

    /// <summary>
    /// Timeouts and limits
    /// </summary>
    public static readonly TimeSpan GlobalTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Neo4j persistence toggle.
    /// set true to write to Neo4j if env vars are present
    /// </summary>
    public const bool PersistToNeo4j = true;
}
