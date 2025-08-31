using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGetGraphSolver.Lib.GraphStore;
using NuGetGraphSolver.Lib.Services;
using NuGetGraphSolver.Lib.Solver;
using NuGetGraphSolver.Lib.UniverseBuilder;
using Pastel;
using System.Drawing;

namespace NuGetGraphSolver;

public sealed class ApplicationRunner
{
    private readonly ILogger<ApplicationRunner> _logger;
    private readonly IPackageMetadataProvider _metadataProvider;

    public ApplicationRunner(ILogger<ApplicationRunner> logger, IPackageMetadataProvider metadataProvider)
    {
        _logger = logger;
        _metadataProvider = metadataProvider;
    }

    public async Task<int> ExecuteAsync(ApplicationOptions options, CancellationToken cancellationToken)
    {
        if (options.PackageIds.Length == 0)
        {
            _logger.LogError("No packages provided.");
            return 2;
        }

        _logger.LogInformation("Using {SourceCount} package source(s): {Sources}", options.PackageSources.Length, string.Join(", ", options.PackageSources));

        var projectFramework = NuGetFramework.ParseFolder(options.TargetFrameworkMoniker);

        var builder = new PackageUniverseBuilder(_metadataProvider, projectFramework);
        var universe = await builder.BuildAsync(options.PackageIds, options.IncludePrerelease, options.MaxVersionsPerPackage, cancellationToken);

        await using var graph = CreateGraphStore(options);
        await graph.UpsertPackageUniverseAsync(universe, cancellationToken);

        var solver = new ZenConfigurationSolver();
        var solution = solver.SolveForNewest(universe);

        // Colored human-readable output (fallback to plain if disabled)
        bool colorEnabled = !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") is null;
        string C(string s, Color c) => colorEnabled ? s.Pastel(c) : s;

        Console.WriteLine(C("Newest compatible selection:", Color.Cyan));
        foreach (var (packageId, (index, version)) in solution.SelectionByPackage.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            var line = $"  {C(packageId, Color.LightGreen)} => {C(version.ToString(), Color.Khaki)} (index {C(index.ToString(), Color.LightGray)})";
            Console.WriteLine(line);
        }

        return 0;
    }

    private static IGraphStore CreateGraphStore(ApplicationOptions o)
    {
        if (!string.IsNullOrWhiteSpace(o.Neo4jUri) && !string.IsNullOrWhiteSpace(o.Neo4jUser) && !string.IsNullOrWhiteSpace(o.Neo4jPassword))
            return new Neo4jGraphStore(o.Neo4jUri!, o.Neo4jUser!, o.Neo4jPassword!);
        return new NullGraphStore();
    }
}