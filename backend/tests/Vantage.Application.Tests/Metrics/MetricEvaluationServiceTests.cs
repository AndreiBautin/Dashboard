using Vantage.Application.Metrics;
using Vantage.Application.Tests.Metrics.Fakes;
using Vantage.Domain.Metrics;
using Vantage.Domain.Metrics.Evaluators;

namespace Vantage.Application.Tests.Metrics;

public class MetricEvaluationServiceTests
{
    private static MetricEvaluatorFactory CreateEvaluatorFactory() => new(
    [
        new IncreaseMetricEvaluator(),
        new DecreaseMetricEvaluator(),
        new StayAboveMetricEvaluator(),
        new StayBelowMetricEvaluator(),
        new StayWithinRangeMetricEvaluator(),
    ]);

    [Fact]
    public async Task EvaluateAsync_DelegatesToTheEvaluatorMatchingTheMetricsStrategy()
    {
        var metricDefinitions = new FakeMetricDefinitionRepository();
        metricDefinitions.Seed(1, new MetricDefinition(
            categoryId: 1, name: "Powerlifting Total", unit: "lb",
            EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var metricSnapshots = new FakeMetricSnapshotRepository();
        metricSnapshots.SeedSnapshots(1,
            new MetricSnapshot(1, 1000, DateTimeOffset.UtcNow.AddMonths(-1)),
            new MetricSnapshot(1, 1050, DateTimeOffset.UtcNow));

        var service = new MetricEvaluationService(metricDefinitions, metricSnapshots, CreateEvaluatorFactory());

        var status = await service.EvaluateAsync(1);

        Assert.Equal(MetricStatus.Improved, status);
    }

    [Fact]
    public async Task EvaluateAsync_WithUnknownMetric_Throws()
    {
        var service = new MetricEvaluationService(
            new FakeMetricDefinitionRepository(), new FakeMetricSnapshotRepository(), CreateEvaluatorFactory());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.EvaluateAsync(999));
    }
}
