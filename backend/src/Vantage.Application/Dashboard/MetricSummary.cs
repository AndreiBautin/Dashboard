using Vantage.Domain.Metrics;

namespace Vantage.Application.Dashboard;

/// <summary>
/// <paramref name="Score"/> is this metric's own 0-100 contribution to its
/// category's score (see MetricScoring) -- the same number
/// <see cref="Metrics.MetricDetail.Score"/> shows on the category's own
/// detail page, surfaced here too so the Dashboard doesn't just show a
/// trend-status word but the number behind it. Null when this metric has
/// nothing to score yet.
/// </summary>
public sealed record MetricSummary(int MetricDefinitionId, string MetricName, MetricStatus Status, int? Score);
