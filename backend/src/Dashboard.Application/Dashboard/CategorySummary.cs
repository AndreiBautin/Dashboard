namespace Dashboard.Application.Dashboard;

public sealed record CategorySummary(
    int CategoryId,
    string CategoryName,
    CategoryStatus Status,
    int? Score,
    IReadOnlyList<MetricSummary> Metrics);
