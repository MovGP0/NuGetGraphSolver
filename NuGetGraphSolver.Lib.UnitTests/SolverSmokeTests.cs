using NuGet.Versioning;
using NuGetGraphSolver.Lib.Domain;
using NuGetGraphSolver.Lib.Solver;
using Shouldly;

namespace NuGetGraphSolver.UnitTests;

public sealed class SolverSmokeTests
{
    /// <summary>
    /// <list type="bullet">
    /// <item>Package A depends on B in [2.0.0, 3.0.0)</item>
    /// <item>A has versions 1.0.0 and 2.0.0</item>
    /// <item>B has versions 1.5.0, 2.1.0</item>
    /// </list>
    /// </summary>
    [Fact]
    public void Chooses_Newest_Without_Conflicts()
    {
        var A = "A"; var B = "B";

        var universe = new PackageUniverse
        {
            TopLevelPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { A },
            CandidatesByPackage = new Dictionary<string, List<PackageVersionNode>>(StringComparer.OrdinalIgnoreCase)
            {
                [A] =
                [
                    new()
                    {
                        PackageId = A, Version = NuGetVersion.Parse("1.0.0"), SourceName = "test",
                        DependencyGroups =
                        [
                            new DependencyGroupInfo
                            {
                                TargetFrameworkMoniker = "any",
                                Dependencies =
                                [
                                    new DependencyInfo
                                        { PackageId = B, VersionRange = VersionRange.Parse("[2.0.0,3.0.0)") }
                                ]
                            }
                        ]
                    },
                    new()
                    {
                        PackageId = A, Version = NuGetVersion.Parse("2.0.0"), SourceName = "test",
                        DependencyGroups =
                        [
                            new DependencyGroupInfo
                            {
                                TargetFrameworkMoniker = "any",
                                Dependencies =
                                [
                                    new DependencyInfo
                                        { PackageId = B, VersionRange = VersionRange.Parse("[2.0.0,3.0.0)") }
                                ]
                            }
                        ]
                    }
                ],
                [B] =
                [
                    new()
                    {
                        PackageId = B, Version = NuGetVersion.Parse("1.5.0"), SourceName = "test",
                        DependencyGroups =
                            [new DependencyGroupInfo { TargetFrameworkMoniker = "any", Dependencies = [] }]
                    },
                    new()
                    {
                        PackageId = B, Version = NuGetVersion.Parse("2.1.0"), SourceName = "test",
                        DependencyGroups =
                            [new DependencyGroupInfo { TargetFrameworkMoniker = "any", Dependencies = [] }]
                    }
                ]
            },
            EffectiveDependencies = new Dictionary<(string, int), List<DependencyInfo>>
            {
                [(A, 0)] = [new DependencyInfo { PackageId = B, VersionRange = VersionRange.Parse("[2.0.0,3.0.0)") }],
                [(A, 1)] = [new DependencyInfo { PackageId = B, VersionRange = VersionRange.Parse("[2.0.0,3.0.0)") }],
                [(B, 0)] = [],
                [(B, 1)] = [],
            }
        };

        var solver = new ZenConfigurationSolver();
        var solution = solver.SolveForNewest(universe);

        solution.SelectionByPackage[A].version.ShouldBe("2.0.0");
        solution.SelectionByPackage[B].version.ShouldBe("2.1.0");
    }
}