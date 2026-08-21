namespace Vantage.Domain.Metrics;

/// <summary>
/// One metric's value as of one monthly review. Belongs to exactly one
/// <see cref="MonthlySnapshot"/> — created via
/// <see cref="MonthlySnapshot.AddMetricSnapshot"/> rather than directly, so
/// the parent aggregate is always the one adding to its own collection.
/// </summary>
public sealed class MetricSnapshot
{
    public int Id { get; private set; }
    public int MetricDefinitionId { get; private set; }
    public int MonthlySnapshotId { get; private set; }
    public decimal Value { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    // For EF Core materialization only.
    private MetricSnapshot()
    {
    }

    public MetricSnapshot(int metricDefinitionId, decimal value, DateTimeOffset recordedAt)
    {
        MetricDefinitionId = metricDefinitionId;
        Value = value;
        RecordedAt = recordedAt;
    }

    /// <summary>
    /// Called only by <see cref="MonthlySnapshot.SetMetricValue"/> when the
    /// caller is re-submitting a value for a metric already recorded this
    /// month (e.g. fixing a typo before moving on) -- updates in place rather
    /// than the parent aggregate adding a second snapshot, which would
    /// violate the one-snapshot-per-metric-per-month uniqueness constraint.
    /// </summary>
    internal void UpdateValue(decimal value, DateTimeOffset recordedAt)
    {
        Value = value;
        RecordedAt = recordedAt;
    }
}
