using Vantage.Domain.Metrics;
using Vantage.Domain.Metrics.Evaluators;

namespace Vantage.Domain.Tests.Metrics;

public class IncreaseMetricEvaluatorTests
{
    private readonly IncreaseMetricEvaluator _evaluator = new();
    private static readonly EvaluationConfig EmptyConfig = new();

    [Fact]
    public void Evaluate_WithFewerThanTwoSnapshots_ReturnsInsufficientData()
    {
        var snapshots = new[] { Snapshot(100) };

        var status = _evaluator.Evaluate(snapshots, EmptyConfig);

        Assert.Equal(MetricStatus.InsufficientData, status);
    }

    [Theory]
    [InlineData(100, 110, MetricStatus.Improved)]
    [InlineData(100, 90, MetricStatus.Regressed)]
    [InlineData(100, 100, MetricStatus.Stagnant)]
    public void Evaluate_ComparesLatestToPrevious(decimal previous, decimal latest, MetricStatus expected)
    {
        var snapshots = new[] { Snapshot(previous), Snapshot(latest) };

        var status = _evaluator.Evaluate(snapshots, EmptyConfig);

        Assert.Equal(expected, status);
    }

    [Fact]
    public void Evaluate_OnlyLooksAtTheLastTwoSnapshots()
    {
        // An older regression shouldn't matter if the most recent month improved.
        var snapshots = new[] { Snapshot(100), Snapshot(50), Snapshot(60) };

        var status = _evaluator.Evaluate(snapshots, EmptyConfig);

        Assert.Equal(MetricStatus.Improved, status);
    }

    private static MetricSnapshot Snapshot(decimal value) =>
        new(metricDefinitionId: 1, value, recordedAt: DateTimeOffset.UtcNow);
}
