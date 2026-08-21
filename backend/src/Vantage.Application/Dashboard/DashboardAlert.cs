namespace Vantage.Application.Dashboard;

/// <summary>
/// <paramref name="Score"/> and <paramref name="Status"/> mirror the same
/// score/status a metric or Social facet carries everywhere else in the
/// Dashboard, so the frontend can render a severity-colored card with a
/// score bar instead of parsing numbers back out of <paramref name="Message"/>.
/// </summary>
public sealed record DashboardAlert(
    int MetricDefinitionId,
    string MetricName,
    string CategoryName,
    string Message,
    int? Score,
    CategoryStatus Status);
