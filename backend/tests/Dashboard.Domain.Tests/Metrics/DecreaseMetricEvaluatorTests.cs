using Dashboard.Domain.Metrics;
using Dashboard.Domain.Metrics.Evaluators;

namespace Dashboard.Domain.Tests.Metrics;

public class DecreaseMetricEvaluatorTests
{
    private readonly DecreaseMetricEvaluator _evaluator = new();
    private static readonly EvaluationConfig EmptyConfig = new();

    [Fact]
    public void Evaluate_WithFewerThanTwoSnapshots_ReturnsInsufficientData()
    {
        var snapshots = new[] { Snapshot(100) };

        var status = _evaluator.Evaluate(snapshots, EmptyConfig);

        Assert.Equal(MetricStatus.InsufficientData, status);
    }

    [Theory]
    [InlineData(100, 90, MetricStatus.Improved)]
    [InlineData(100, 110, MetricStatus.Regressed)]
    [InlineData(100, 100, MetricStatus.Stagnant)]
    public void Evaluate_ComparesLatestToPrevious(decimal previous, decimal latest, MetricStatus expected)
    {
        var snapshots = new[] { Snapshot(previous), Snapshot(latest) };

        var status = _evaluator.Evaluate(snapshots, EmptyConfig);

        Assert.Equal(expected, status);
    }

    private static MetricSnapshot Snapshot(decimal value) =>
        new(metricDefinitionId: 1, value, recordedAt: DateTimeOffset.UtcNow);
}
