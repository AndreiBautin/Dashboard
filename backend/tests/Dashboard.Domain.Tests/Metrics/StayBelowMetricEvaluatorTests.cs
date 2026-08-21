using Dashboard.Domain.Metrics;
using Dashboard.Domain.Metrics.Evaluators;

namespace Dashboard.Domain.Tests.Metrics;

public class StayBelowMetricEvaluatorTests
{
    private readonly StayBelowMetricEvaluator _evaluator = new();
    private static readonly EvaluationConfig ThresholdOf2000 = new(Threshold: 2000);

    [Fact]
    public void Evaluate_WithFewerThanTwoSnapshots_ReturnsInsufficientData()
    {
        var snapshots = new[] { Snapshot(1500) };

        var status = _evaluator.Evaluate(snapshots, ThresholdOf2000);

        Assert.Equal(MetricStatus.InsufficientData, status);
    }

    [Fact]
    public void Evaluate_WithoutThreshold_Throws()
    {
        var snapshots = new[] { Snapshot(1500), Snapshot(1600) };

        Assert.Throws<InvalidOperationException>(() => _evaluator.Evaluate(snapshots, new EvaluationConfig()));
    }

    [Theory]
    [InlineData(2200, 1800, MetricStatus.Improved)]    // crossed back below the threshold
    [InlineData(1500, 1600, MetricStatus.Stagnant)]    // stayed below, nothing new to report
    [InlineData(1500, 2200, MetricStatus.Regressed)]   // rose above the threshold
    [InlineData(2200, 2300, MetricStatus.Regressed)]   // stayed above
    public void Evaluate_ComparesAgainstThreshold(decimal previous, decimal latest, MetricStatus expected)
    {
        var snapshots = new[] { Snapshot(previous), Snapshot(latest) };

        var status = _evaluator.Evaluate(snapshots, ThresholdOf2000);

        Assert.Equal(expected, status);
    }

    private static MetricSnapshot Snapshot(decimal value) =>
        new(metricDefinitionId: 1, value, recordedAt: DateTimeOffset.UtcNow);
}
