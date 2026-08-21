using Dashboard.Application.Metrics;
using Dashboard.Application.Tests.Metrics.Fakes;
using Dashboard.Domain.Metrics;

namespace Dashboard.Application.Tests.Metrics;

public class MetricTrendServiceTests
{
    [Fact]
    public async Task GetTrendAsync_ReturnsThePointsFromTheRepository()
    {
        var metricDefinitions = new FakeMetricDefinitionRepository();
        metricDefinitions.Seed(1, new MetricDefinition(
            categoryId: 1, name: "Net Worth", unit: "USD",
            EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var metricSnapshots = new FakeMetricSnapshotRepository();
        var expectedPoints = new[]
        {
            new MetricTrendPoint(new DateOnly(2026, 6, 1), 50_000m),
            new MetricTrendPoint(new DateOnly(2026, 7, 1), 52_000m),
        };
        metricSnapshots.SeedTrend(1, expectedPoints);

        var service = new MetricTrendService(metricDefinitions, metricSnapshots);

        var points = await service.GetTrendAsync(1);

        Assert.Equal(expectedPoints, points);
    }

    [Fact]
    public async Task GetTrendAsync_WithUnknownMetric_Throws()
    {
        var service = new MetricTrendService(new FakeMetricDefinitionRepository(), new FakeMetricSnapshotRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetTrendAsync(999));
    }
}
