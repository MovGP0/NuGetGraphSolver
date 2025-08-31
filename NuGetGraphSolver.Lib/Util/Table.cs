using NuGetGraphSolver.Lib.Domain;
using NuGetGraphSolver.Lib.Solver;
using Microsoft.Extensions.Logging;
using Pastel; // for coloring
using System.Text;

namespace NuGetGraphSolver.Lib.Util;

public static class Table
{
    public static void DumpSelection(string title, ZenConfigurationSolver.Solution s, PackageUniverse uni, ILogger logger)
    {
        if (logger == null) return;

        var sb = new StringBuilder();

        if (s.SelectionByPackage.Count == 0)
        {
            sb.Append(($"{title}: (no packages)").Pastel("#888888"));
            logger.LogInformation("{Table}", sb.ToString());
            return;
        }

        var rows = s.SelectionByPackage
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv =>
            {
                var pid = kv.Key;
                var idx = kv.Value.index;
                var ver = kv.Value.version;
                var source = uni.CandidatesByPackage[pid][idx].SourceName;
                return new Row(pid, ver, source, idx);
            })
            .ToList();

        var wId = Math.Max("PackageId".Length, rows.Max(r => r.PackageId.Length));
        var wVer = Math.Max("Version".Length, rows.Max(r => r.Version.Length));
        var wSource = Math.Max("Source".Length, rows.Max(r => r.Source.Length));
        var wIdx = Math.Max("Index".Length, rows.Max(r => r.Index.ToString().Length));

        string ColorPackage(string pid) => pid.PadRight(wId).Pastel("#00afff");
        string ColorVersion(string ver)
        {
            var baseStr = ver.PadRight(wVer);
            if (IsPreRelease(ver)) return baseStr.Pastel("#ffa500");
            return baseStr.Pastel("#00d75f");
        }
        string ColorSource(string src) => src.PadRight(wSource).Pastel("#afafff");
        string ColorIndex(int idx) => idx.ToString().PadLeft(wIdx).Pastel("#ffd700");

        string BuildSeparator()
        {
            return new string('-', wId).Pastel("#444444") + "  " +
                   new string('-', wVer).Pastel("#444444") + "  " +
                   new string('-', wSource).Pastel("#444444") + "  " +
                   new string('-', wIdx).Pastel("#444444");
        }

        string BuildRow(Row r, bool colorize)
        {
            if (!colorize)
                return r.PackageId.PadRight(wId) + "  " + r.Version.PadRight(wVer) + "  " + r.Source.PadRight(wSource) + "  " + r.Index.ToString().PadLeft(wIdx);
            return ColorPackage(r.PackageId) + "  " + ColorVersion(r.Version) + "  " + ColorSource(r.Source) + "  " + ColorIndex(r.Index);
        }

        // Header
        var headerTitle = $"== {title} == (packages={rows.Count}, objective={s.ObjectiveValue})";
        sb.AppendLine(headerTitle.Pastel("#ffffff").PastelBg("#005f87"));
        sb.AppendLine(BuildRow(new Row("PackageId", "Version", "Source", 0), colorize: true).PastelBg("#303030"));
        sb.AppendLine(BuildSeparator());
        foreach (var r in rows)
            sb.AppendLine(BuildRow(r, colorize: true));

        logger.LogInformation("{Table}", sb.ToString().TrimEnd());
    }

    private static bool IsPreRelease(string version) => version.Contains('-', StringComparison.Ordinal);

    private sealed record Row(string PackageId, string Version, string Source, int Index);
}