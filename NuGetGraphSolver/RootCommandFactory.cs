using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NuGetGraphSolver.Lib.Services;

namespace NuGetGraphSolver;

internal static class RootCommandFactory
{
    public static RootCommand Create(IServiceCollection services)
    {
        var packages = new Option<string[]>("--packages", "-p")
        {
            AllowMultipleArgumentsPerToken = true,
            Required = true,
            Description = "List of NuGet package IDs (repeat or comma-separated)."
        };

        var targetFramework = new Option<string>("--tfm")
        {
            Required = true,
            Description = "Target Framework Moniker (e.g., net8.0)."
        };

        var sources = new Option<string[]>("--source")
        {
            AllowMultipleArgumentsPerToken = true,
            Description = "NuGet v3 sources (repeatable)."
        };

        var includePrerelease = new Option<bool>("--include-prerelease")
        {
            Description = "Include prerelease versions."
        };

        var maxVersions = new Option<int>("--max-versions")
        {
            Description = "Max versions per package to consider."
        };

        var neo4jUri = new Option<string>("--neo4j-uri") { Description = "Neo4j Bolt URI (e.g., bolt://localhost:7687)." };
        var neo4jUser = new Option<string>("--neo4j-user") { Description = "Neo4j username" };
        var neo4jPassword = new Option<string>("--neo4j-password") { Description = "Neo4j password" };

        var root = new RootCommand("NuGet Graph Solver");
        root.Options.Add(packages);
        root.Options.Add(targetFramework);
        root.Options.Add(sources);
        root.Options.Add(includePrerelease);
        root.Options.Add(maxVersions);
        root.Options.Add(neo4jUri);
        root.Options.Add(neo4jUser);
        root.Options.Add(neo4jPassword);

        root.SetAction(parseResult =>
        {
            var pkgs = parseResult.GetValue(packages) ?? [];
            var tfm = parseResult.GetValue(targetFramework)!; // required
            var srcs = parseResult.GetValue(sources) ?? [];
            var pre = parseResult.GetValue(includePrerelease);
            var max = parseResult.GetValue(maxVersions);
            if (max <= 0) max = 20;
            var u = parseResult.GetValue(neo4jUri);
            var usr = parseResult.GetValue(neo4jUser);
            var pwd = parseResult.GetValue(neo4jPassword);

            var options = new ApplicationOptions
            {
                PackageIds = pkgs.SelectMany(p => p.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                TargetFrameworkMoniker = tfm,
                PackageSources = (srcs.Length > 0 ? srcs : ["https://api.nuget.org/v3/index.json"]),
                IncludePrerelease = pre,
                MaxVersionsPerPackage = max,
                Neo4jUri = string.IsNullOrWhiteSpace(u) ? null : u,
                Neo4jUser = string.IsNullOrWhiteSpace(usr) ? null : usr,
                Neo4jPassword = string.IsNullOrWhiteSpace(pwd) ? null : pwd
            };

            // Register the metadata provider now with the resolved package sources.
            services.AddSingleton<IPackageMetadataProvider>(_ => new NuGetMetadataProvider(options.PackageSources));

            var sp = services.BuildServiceProvider();
            var runner = sp.GetRequiredService<ApplicationRunner>();
            var exit = runner.ExecuteAsync(options, CancellationToken.None).GetAwaiter().GetResult();
            return exit;
        });

        return root;
    }
}