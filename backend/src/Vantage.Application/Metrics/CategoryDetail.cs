using Vantage.Application.Dashboard;

namespace Vantage.Application.Metrics;

/// <summary>
/// <paramref name="Score"/> is the same 0-100 number the Dashboard shows for
/// this category (see MetricScoring/DashboardService) -- surfaced here too
/// so the category's own detail page doesn't have to send you back to the
/// Dashboard just to see how it's scoring. Null when no metric in the
/// category has anything to score yet. <paramref name="Status"/> is that
/// same score's qualitative read (Good/NeedsAttention/Regressed/NoData),
/// via the identical thresholds <see cref="CategoryStatusCalculator"/> uses
/// for the Dashboard's category cards -- an "overall rating" for the
/// category, not just a bare number.
/// </summary>
public sealed record CategoryDetail(
    int CategoryId,
    string CategoryName,
    int? Score,
    CategoryStatus Status,
    IReadOnlyList<MetricDetail> Metrics);
