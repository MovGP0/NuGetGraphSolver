using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neo4j.Driver;
using Testcontainers.Neo4j;
using NuGetGraphSolver.Lib.Services;
using Docker.DotNet;
using DotNet.Testcontainers.Builders;
using Xunit.Sdk;

namespace NuGetGraphSolver.IntegrationTests;

public sealed class GraphEndToEndTests : IAsyncLifetime
{
    private Neo4jContainer? _neo4jContainer;
    private string? _boltUri;
    private const string Username = "neo4j";
    private const string Password = "testpass";
    private bool _skip;

    public async Task InitializeAsync()
    {
        if (!await IsDockerAvailableAsync())
        {
            _skip = true;
            return;
        }

        _neo4jContainer = new Neo4jBuilder()
            .WithImage("neo4j:5")
            .WithEnvironment("NEO4J_AUTH", $"{Username}/{Password}")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(7687))
            .Build();

        await _neo4jContainer.StartAsync();
        _boltUri = $"bolt://{_neo4jContainer.Hostname}:{_neo4jContainer.GetMappedPublicPort(7687)}";
    }

    public async Task DisposeAsync()
    {
        if (_neo4jContainer is not null)
        {
            await _neo4jContainer.DisposeAsync();
        }
    }

    private static async Task<bool> IsDockerAvailableAsync()
    {
        try
        {
            // Use default configuration (env vars/standard endpoints)
            using var cfg = new DockerClientConfiguration();
            using var client = cfg.CreateClient();
            await client.System.PingAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public async Task BuildGraph_And_Solve_Newest_Selection()
    {
        if (_skip)
            throw SkipException.ForSkip("Docker engine not available – integration test skipped.");

        // Arrange: DI like Program.cs, but we wire services directly
        var builder = Host.CreateApplicationBuilder();
        ServicesRegistration.AddCoreServices(builder.Services);
        // Register metadata provider with default source
        builder.Services.AddSingleton<IPackageMetadataProvider>(_ => new NuGetMetadataProvider(["https://api.nuget.org/v3/index.json"]));
        using var sp = builder.Services.BuildServiceProvider();

        var runner = sp.GetRequiredService<ApplicationRunner>();

        var options = new ApplicationOptions
        {
            PackageIds = ["Serilog", "Serilog.Sinks.Console"],
            TargetFrameworkMoniker = "net8.0",
            PackageSources = ["https://api.nuget.org/v3/index.json"],
            IncludePrerelease = false,
            MaxVersionsPerPackage = 10,
            Neo4jUri = _boltUri,
            Neo4jUser = Username,
            Neo4jPassword = Password
        };

        // Act
        var exit = await runner.ExecuteAsync(options, CancellationToken.None);

        // Assert exit code
        exit.ShouldBe(0);

        // Assert graph was written: query some nodes/relationships
        var driver = GraphDatabase.Driver(_boltUri, AuthTokens.Basic(Username, Password));
        await using (driver)
        await using (var session = driver.AsyncSession())
        {
            var countVersions = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("MATCH (v:PackageVersion)-[:VERSION_OF]->(:Package) RETURN count(v) as c");
                var rec = await cursor.SingleAsync();
                return rec["c"].As<long>();
            });

            countVersions.ShouldBeGreaterThan(0);

            var countDeps = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("MATCH (:PackageVersion)-[r:DEPENDS_ON]->(:Package) RETURN count(r) as c");
                var rec = await cursor.SingleAsync();
                return rec["c"].As<long>();
            });

            countDeps.ShouldBeGreaterThan(0);
        }
    }
}
