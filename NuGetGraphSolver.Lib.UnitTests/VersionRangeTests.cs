using NuGet.Versioning;
using Shouldly;

namespace NuGetGraphSolver.UnitTests;

public sealed class VersionRangeTests
{
    [Theory]
    [InlineData("1.2.3", "[1.2.3,2.0.0)", true)]
    [InlineData("2.0.0", "[1.2.3,2.0.0)", false)]
    [InlineData("1.2.3-alpha.1", "[1.2.3, )", false)] // default excludes prerelease unless specified
    public void Range_Satisfies(string version, string range, bool expected)
    {
        var v = NuGetVersion.Parse(version);
        var r = VersionRange.Parse(range);
        r.Satisfies(v).ShouldBe(expected);
    }
}