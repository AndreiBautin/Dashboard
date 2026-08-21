using Dashboard.Domain.Metrics;

namespace Dashboard.Application.Metrics;

/// <summary>
/// Cross-cutting reads over <see cref="MetricSnapshot"/>s for a single
/// metric, spanning every <see cref="MonthlySnapshot"/> they belong to.
/// Writes still go through <see cref="MonthlySnapshot.AddMetricSnapshot"/>
/// and <see cref="IMonthlySnapshotRepository"/> — this is a read-side
/// convenience for evaluation and trend queries.
/// </summary>
public interface IMetricSnapshotRepository
{
    /// <summary>
    /// All snapshots for one metric, ordered oldest to newest by the review
    /// month they belong to (not by when they were recorded).
    /// </summary>
    Task<IReadOnlyList<MetricSnapshot>> GetForMetricAsync(int metricDefinitionId, CancellationToken cancellationToken = default);

    /// <summary>The same data shaped for charting — (month, value) pairs.</summary>
    Task<IReadOnlyList<MetricTrendPoint>> GetTrendForMetricAsync(int metricDefinitionId, CancellationToken cancellationToken = default);
}
