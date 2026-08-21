using Vantage.Domain.Metrics;

namespace Vantage.Domain.Tests.Metrics;

public class MonthlySnapshotTests
{
    [Fact]
    public void Constructor_NormalizesMonthToTheFirst()
    {
        var snapshot = new MonthlySnapshot(new DateOnly(2026, 7, 15), DateTimeOffset.UtcNow);

        Assert.Equal(new DateOnly(2026, 7, 1), snapshot.Month);
    }

    [Fact]
    public void AddMetricSnapshot_AddsToTheCollection()
    {
        var snapshot = new MonthlySnapshot(new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow);

        var metricSnapshot = snapshot.AddMetricSnapshot(metricDefinitionId: 1, value: 1050, DateTimeOffset.UtcNow);

        Assert.Single(snapshot.MetricSnapshots);
        Assert.Same(metricSnapshot, snapshot.MetricSnapshots[0]);
    }

    [Fact]
    public void AddMetricSnapshot_CanRecordMultipleMetrics()
    {
        var snapshot = new MonthlySnapshot(new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow);

        snapshot.AddMetricSnapshot(metricDefinitionId: 1, value: 1050, DateTimeOffset.UtcNow);
        snapshot.AddMetricSnapshot(metricDefinitionId: 2, value: 15.25m, DateTimeOffset.UtcNow);

        Assert.Equal(2, snapshot.MetricSnapshots.Count);
    }

    [Fact]
    public void SetMetricValue_WithNoExistingSnapshotForTheMetric_AddsANewOne()
    {
        var snapshot = new MonthlySnapshot(new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow);

        snapshot.SetMetricValue(metricDefinitionId: 1, value: 1050, DateTimeOffset.UtcNow);

        var recorded = Assert.Single(snapshot.MetricSnapshots);
        Assert.Equal(1, recorded.MetricDefinitionId);
        Assert.Equal(1050, recorded.Value);
    }

    [Fact]
    public void SetMetricValue_WithAnExistingSnapshotForTheMetric_UpdatesItInPlaceRatherThanDuplicating()
    {
        var snapshot = new MonthlySnapshot(new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow);
        var original = snapshot.AddMetricSnapshot(metricDefinitionId: 1, value: 1050, DateTimeOffset.UtcNow.AddMinutes(-5));

        var updatedAt = DateTimeOffset.UtcNow;
        snapshot.SetMetricValue(metricDefinitionId: 1, value: 1075, updatedAt);

        var recorded = Assert.Single(snapshot.MetricSnapshots);
        Assert.Same(original, recorded);
        Assert.Equal(1075, recorded.Value);
        Assert.Equal(updatedAt, recorded.RecordedAt);
    }

    [Fact]
    public void SetMetricValue_OnlyUpdatesTheMatchingMetric()
    {
        var snapshot = new MonthlySnapshot(new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow);
        snapshot.AddMetricSnapshot(metricDefinitionId: 1, value: 1050, DateTimeOffset.UtcNow);
        snapshot.AddMetricSnapshot(metricDefinitionId: 2, value: 15.0m, DateTimeOffset.UtcNow);

        snapshot.SetMetricValue(metricDefinitionId: 1, value: 1075, DateTimeOffset.UtcNow);

        Assert.Equal(2, snapshot.MetricSnapshots.Count);
        Assert.Equal(15.0m, snapshot.MetricSnapshots.Single(s => s.MetricDefinitionId == 2).Value);
    }

    [Fact]
    public void SetSocialSnapshot_WithNoExistingSocialSnapshot_CreatesOne()
    {
        var snapshot = new MonthlySnapshot(new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow);

        snapshot.SetSocialSnapshot(9);

        Assert.NotNull(snapshot.SocialSnapshot);
        Assert.Equal(9, snapshot.SocialSnapshot!.ActiveFriendCount);
    }

    [Fact]
    public void SetSocialSnapshot_WhenCalledAgain_UpdatesInPlaceRatherThanReplacing()
    {
        var snapshot = new MonthlySnapshot(new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow);
        snapshot.SetSocialSnapshot(9);
        var original = snapshot.SocialSnapshot;

        snapshot.SetSocialSnapshot(10);

        Assert.Same(original, snapshot.SocialSnapshot);
        Assert.Equal(10, snapshot.SocialSnapshot!.ActiveFriendCount);
    }
}
