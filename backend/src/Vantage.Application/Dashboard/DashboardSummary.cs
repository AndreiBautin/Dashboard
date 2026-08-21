namespace Vantage.Application.Dashboard;

/// <summary>
/// Everything the home dashboard needs for one render.
/// <paramref name="OverallStatus"/> is <paramref name="OverallScore"/>'s
/// qualitative read, via the same <see cref="CategoryStatusCalculator"/>
/// thresholds each individual category's <see cref="CategorySummary.Status"/>
/// uses -- so "how am I doing overall?" gets an answer in the same
/// Good/NeedsAttention/Regressed vocabulary as every category beneath it,
/// not just a bare number.
/// </summary>
public sealed record DashboardSummary(
    int? OverallScore,
    CategoryStatus OverallStatus,
    IReadOnlyList<CategorySummary> Categories,
    IReadOnlyList<DashboardAlert> Alerts);
