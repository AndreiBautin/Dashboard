using Dashboard.Domain.Social;

namespace Dashboard.Domain.Metrics;

/// <summary>
/// The aggregate root representing one monthly review session. All of that
/// month's <see cref="MetricSnapshot"/>s are added through here, keeping the
/// "one review, many metrics recorded together" invariant in one place. Also
/// carries the one <see cref="Social.SocialSnapshot"/> for the month, set
/// through <see cref="SetSocialSnapshot"/>.
/// </summary>
public sealed class MonthlySnapshot
{
    private readonly List<MetricSnapshot> _metricSnapshots = [];

    public int Id { get; private set; }

    /// <summary>Always normalized to the first of the month.</summary>
    public DateOnly Month { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<MetricSnapshot> MetricSnapshots => _metricSnapshots;

    public SocialSnapshot? SocialSnapshot { get; private set; }

    // For EF Core materialization only.
    private MonthlySnapshot()
    {
    }

    public MonthlySnapshot(DateOnly month, DateTimeOffset createdAt)
    {
        Month = new DateOnly(month.Year, month.Month, 1);
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Records a metric's value for this review. EF Core assigns the
    /// resulting snapshot's foreign key when the aggregate is saved — callers
    /// never set <see cref="MetricSnapshot.MonthlySnapshotId"/> themselves.
    /// </summary>
    public MetricSnapshot AddMetricSnapshot(int metricDefinitionId, decimal value, DateTimeOffset recordedAt)
    {
        var snapshot = new MetricSnapshot(metricDefinitionId, value, recordedAt);
        _metricSnapshots.Add(snapshot);
        return snapshot;
    }

    /// <summary>
    /// Records a metric's value for this review, same as
    /// <see cref="AddMetricSnapshot"/>, except that re-submitting a value for
    /// a metric already recorded this month updates that snapshot in place
    /// instead of adding a second one -- callers (entry screens) don't need
    /// to know whether this month's value already exists.
    /// </summary>
    public MetricSnapshot SetMetricValue(int metricDefinitionId, decimal value, DateTimeOffset recordedAt)
    {
        var existing = _metricSnapshots.Find(s => s.MetricDefinitionId == metricDefinitionId);
        if (existing is not null)
        {
            existing.UpdateValue(value, recordedAt);
            return existing;
        }

        return AddMetricSnapshot(metricDefinitionId, value, recordedAt);
    }

    /// <summary>
    /// Records this month's active-circle size, same upsert shape as
    /// <see cref="SetMetricValue"/>: updates the existing
    /// <see cref="Social.SocialSnapshot"/> in place if this month already has
    /// one, otherwise creates it.
    /// </summary>
    public SocialSnapshot SetSocialSnapshot(int activeFriendCount)
    {
        if (SocialSnapshot is not null)
        {
            SocialSnapshot.UpdateActiveFriendCount(activeFriendCount);
            return SocialSnapshot;
        }

        SocialSnapshot = new SocialSnapshot(activeFriendCount);
        return SocialSnapshot;
    }
}
