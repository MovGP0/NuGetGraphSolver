using NuGet.Frameworks;
using Shouldly;

namespace NuGetGraphSolver.UnitTests;

public sealed class FrameworkSelectionTests
{
    [Fact]
    public void Net8_Compatible_With_NetStandard2()
    {
        var net8 = NuGetFramework.ParseFolder("net8.0");
        var ns2 = NuGetFramework.ParseFolder("netstandard2.0");

        var compat = DefaultCompatibilityProvider.Instance.IsCompatible(net8, ns2);
        compat.ShouldBeTrue(); // .NET 8 can consume netstandard2.0
    }

    [Fact]
    public void GetNearest_Prefers_More_Specific()
    {
        var reducer = new FrameworkReducer();
        var project = NuGetFramework.ParseFolder("net8.0");
        var candidates = new[]
        {
            NuGetFramework.ParseFolder("netstandard2.0"),
            NuGetFramework.ParseFolder("net6.0"),
        };

        var nearest = reducer.GetNearest(project, candidates);
        nearest.GetShortFolderName().ShouldBe("net6.0"); // specific .NET beats .NET Standard when compatible
    }
}