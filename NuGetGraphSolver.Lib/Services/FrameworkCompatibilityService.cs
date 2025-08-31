using NuGet.Frameworks;
using NuGetGraphSolver.Lib.Domain;

namespace NuGetGraphSolver.Lib.Services;

public static class FrameworkCompatibilityService
{
    private static readonly FrameworkReducer Reducer = new();
    private static readonly IFrameworkNameProvider NameProvider = DefaultFrameworkNameProvider.Instance;
    private static readonly IFrameworkCompatibilityProvider Compatibility = DefaultCompatibilityProvider.Instance;

    public static NuGetFramework Parse(string tfm) =>
        NuGetFramework.ParseFolder(tfm);

    /// Select the dependency group for the package version that is "nearest" to the project framework.
    public static DependencyGroupInfo? SelectNearestDependencyGroup(
        IEnumerable<DependencyGroupInfo> groups, NuGetFramework projectFramework)
    {
        var groupList = groups?.ToList() ?? [];

        if (groupList.Count == 0)
            return new DependencyGroupInfo
            {
                TargetFrameworkMoniker = NuGetFramework.AnyFramework.GetShortFolderName(),
                Dependencies = []
            };

        // Map to NuGetFrameworks
        var candidateFrameworks = groupList.Select(g =>
                NuGetFramework.ParseFolder(g.TargetFrameworkMoniker))
            .ToList();

        var nearest = Reducer.GetNearest(projectFramework, candidateFrameworks);

        if (nearest == null)
        {
            // Check if any group is Any
            var any = groupList.FirstOrDefault(g =>
                NuGetFramework.ParseFolder(g.TargetFrameworkMoniker).IsAny);
            return any; // might be null
        }

        return groupList.First(g =>
            NuGetFramework.ParseFolder(g.TargetFrameworkMoniker).Equals(nearest));
    }

    /// Human-readable inference note about .NET vs .NET Standard.
    public static string[] InferenceCheatSheet =>
    [
        ".NET 5–8 implement .NET Standard 2.1 (and can consume .NET Standard 2.0).",
        ".NET 8.0 projects can reference libraries targeting netstandard2.0 and netstandard2.1.",
        "NuGet’s 'nearest framework' selection prefers the most specific compatible asset group."
    ];
}