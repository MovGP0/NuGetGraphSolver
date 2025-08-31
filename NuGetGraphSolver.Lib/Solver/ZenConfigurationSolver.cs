using NuGetGraphSolver.Lib.Domain;
using ZenLib;

namespace NuGetGraphSolver.Lib.Solver;

public sealed class ZenConfigurationSolver
{
    public sealed class Solution
    {
        public required Dictionary<string, (int index, string version)> SelectionByPackage { get; init; }
        public required double ObjectiveValue { get; init; }
    }

    public Solution SolveForNewest(PackageUniverse universe, HashSet<string>? optimizeThesePackages = null, TimeSpan? timeout = null)
    {
        // Variables: for each package, an index [0..n-1] picking the chosen version (ascending order = newer has larger index)
        var indexVars = new Dictionary<string, Zen<int>>(StringComparer.OrdinalIgnoreCase);
        var allConstraints = new List<Zen<bool>>();

        foreach (var (pid, versions) in universe.CandidatesByPackage)
        {
            var idx = Zen.Symbolic<int>();
            indexVars[pid] = idx;

            var upper = versions.Count - 1;
            // Domain: 0 <= idx <= upper (only if we actually have candidates)
            allConstraints.Add(idx >= 0 & idx <= upper);
        }

        // Dependency constraints: if (pid == i) => depVar in AllowedIndices
        foreach (var (key, deps) in universe.EffectiveDependencies)
        {
            var (pid, i) = key;
            var selector = indexVars[pid] == i;

            foreach (var dep in deps)
            {
                if (!universe.CandidatesByPackage.TryGetValue(dep.PackageId, out var depVersions) || depVersions.Count == 0)
                {
                    // Impossible dependency -> block this version
                    allConstraints.Add(Implies(selector, Zen.Constant(false)));
                    continue;
                }

                // Build set of allowed indices by version-range
                Zen<bool> anyAllowed = Zen.Constant(false);
                for (var j = 0; j < depVersions.Count; j++)
                {
                    var v = depVersions[j].Version;
                    if (dep.VersionRange == null || dep.VersionRange.Satisfies(v))
                    {
                        anyAllowed = anyAllowed | (indexVars[dep.PackageId] == j);
                    }
                }

                allConstraints.Add(Implies(selector, anyAllowed));
            }
        }

        // Objective: maximize sum of indices for *top-level* packages (you can extend to all packages if desired)
        var top = optimizeThesePackages ?? universe.TopLevelPackages;
        var objective = top
            .Select(pid => indexVars[pid])
            .Aggregate(Zen.Constant(0), (acc, x) => acc + x);

        // Conjoin constraints
        var all = allConstraints.Aggregate(Zen.Constant(true), (acc, c) => acc & c);

        var config = new ZenLib.Solver.SolverConfig
        {
            SolverType = ZenLib.Solver.SolverType.Z3,
            SolverTimeout = timeout ?? TimeSpan.FromSeconds(30),
        };

        var sol = Zen.Maximize(objective: objective, subjectTo: all, config: config);

        // Project back
        var pick = new Dictionary<string, (int index, string version)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (pid, versions) in universe.CandidatesByPackage)
        {
            var chosenIndex = sol.Get(indexVars[pid]);
            var chosenVersion = versions[chosenIndex].Version.ToNormalizedString();
            pick[pid] = (chosenIndex, chosenVersion);
        }

        var obj = Convert.ToDouble(sol.Get(objective));

        return new Solution
        {
            SelectionByPackage = pick,
            ObjectiveValue = obj
        };
    }

    private static Zen<bool> Implies(Zen<bool> a, Zen<bool> b) => Zen.Or(Zen.Not(a), b);
}